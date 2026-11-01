using fcg_users_api.Events.DomainEvents;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace fcg_users_api.Events.RabbitMQ;

/// <summary>
/// Implementação do publicador de eventos usando RabbitMQ
/// </summary>
public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly string _exchangeName;
    private bool _disposed = false;

    public RabbitMQEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMQEventPublisher> logger)
    {
        _logger = logger;
        _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "fcg.users.events";

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        try
        {
            _connection = factory.CreateConnection("FCG-Users-API-Publisher");
            _channel = _connection.CreateModel();

            // Declara exchange do tipo topic para roteamento flexível
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            _logger.LogInformation(
                "RabbitMQ conectado com sucesso. Exchange: {Exchange}, Host: {Host}",
                _exchangeName,
                factory.HostName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar ao RabbitMQ");
            throw;
        }
    }

    public async Task PublicarAsync<TEvent>(TEvent evento, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQEventPublisher));

        try
        {
            var routingKey = GerarRoutingKey(evento.EventType);
            var mensagem = SerializarEvento(evento);
            var body = Encoding.UTF8.GetBytes(mensagem);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Mensagens persistentes
            properties.ContentType = "application/json";
            properties.MessageId = evento.EventId.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = evento.EventType;
            properties.AppId = "fcg-users-api";

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation(
                "Evento publicado: {EventType} | ID: {EventId} | RoutingKey: {RoutingKey}",
                evento.EventType,
                evento.EventId,
                routingKey
            );

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao publicar evento {EventType} | ID: {EventId}",
                evento.EventType,
                evento.EventId
            );
            throw;
        }
    }

    public async Task PublicarBatchAsync(IEnumerable<IDomainEvent> eventos, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQEventPublisher));

        var batch = _channel.CreateBasicPublishBatch();

        try
        {
            foreach (var evento in eventos)
            {
                var routingKey = GerarRoutingKey(evento.EventType);
                var mensagem = SerializarEvento(evento);
                var body = Encoding.UTF8.GetBytes(mensagem);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = evento.EventId.ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.Type = evento.EventType;
                properties.AppId = "fcg-users-api";

                batch.Add(_exchangeName, routingKey, false, properties, body);
            }

            batch.Publish();

            _logger.LogInformation(
                "Batch de {Count} eventos publicados com sucesso",
                eventos.Count()
            );

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar batch de eventos");
            throw;
        }
    }

    private string GerarRoutingKey(string eventType)
    {
        // Padrão: users.{action}
        // Exemplos: users.created, users.activated, users.profile.changed
        return eventType switch
        {
            "UsuarioCriado" => "users.created",
            "ContaAtivada" => "users.activated",
            "PerfilAlterado" => "users.profile.changed",
            "SenhaAlterada" => "users.password.changed",
            _ => $"users.{eventType.ToLower()}"
        };
    }

    private string SerializarEvento<TEvent>(TEvent evento) where TEvent : IDomainEvent
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        return JsonSerializer.Serialize(evento, options);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();

            _logger.LogInformation("Conexão RabbitMQ fechada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fechar conexão RabbitMQ");
        }
        finally
        {
            _disposed = true;
        }
    }
}
