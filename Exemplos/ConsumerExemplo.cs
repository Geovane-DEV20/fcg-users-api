using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using fcg_users_api.Events.DomainEvents;

namespace fcg_users_api.Exemplos;

/// <summary>
/// Exemplo de consumer para testar a recepção de eventos do RabbitMQ
/// Este é apenas um exemplo educacional - não será executado na API
/// </summary>
public class ConsumerExemplo
{
    private readonly ILogger<ConsumerExemplo> _logger;
    private readonly IConfiguration _configuration;

    public ConsumerExemplo(ILogger<ConsumerExemplo> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Consumer de exemplo que escuta TODOS os eventos de usuários
    /// </summary>
    public void IniciarConsumerTodosEventos()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        using var connection = factory.CreateConnection("Test-Consumer");
        using var channel = connection.CreateModel();

        var exchangeName = _configuration["RabbitMQ:ExchangeName"] ?? "fcg.users.events";

        // Declarar fila temporária
        var queueName = channel.QueueDeclare(
            queue: "test.consumer.all.events",
            durable: true,
            exclusive: false,
            autoDelete: false
        ).QueueName;

        // Bind para receber TODOS os eventos de usuários (users.*)
        channel.QueueBind(
            queue: queueName,
            exchange: exchangeName,
            routingKey: "users.*"
        );

        _logger.LogInformation("Consumer iniciado. Aguardando eventos...");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                var messageId = ea.BasicProperties.MessageId;

                _logger.LogInformation(
                    "📩 EVENTO RECEBIDO | RoutingKey: {RoutingKey} | MessageId: {MessageId}",
                    routingKey,
                    messageId
                );
                _logger.LogInformation("Payload: {Message}", message);

                // Processar evento baseado no tipo
                switch (routingKey)
                {
                    case "users.created":
                        ProcessarUsuarioCriado(message);
                        break;
                    case "users.activated":
                        ProcessarContaAtivada(message);
                        break;
                    case "users.password.changed":
                        ProcessarSenhaAlterada(message);
                        break;
                    case "users.profile.changed":
                        ProcessarPerfilAlterado(message);
                        break;
                }

                // Confirma o processamento da mensagem
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento");
                // Rejeita e recoloca na fila
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );

        Console.WriteLine("Pressione ENTER para parar o consumer...");
        Console.ReadLine();
    }

    private void ProcessarUsuarioCriado(string json)
    {
        var evento = JsonSerializer.Deserialize<UsuarioCriadoEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _logger.LogInformation(
            "✅ Usuário Criado | ID: {UsuarioId} | Nome: {Nome} | Email: {Email}",
            evento?.UsuarioId,
            evento?.Nome,
            evento?.Email
        );

        // Aqui você pode:
        // - Enviar email de boas-vindas
        // - Criar biblioteca vazia em outro microsserviço
        // - Registrar no sistema de analytics
    }

    private void ProcessarContaAtivada(string json)
    {
        var evento = JsonSerializer.Deserialize<ContaAtivadaEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _logger.LogInformation(
            "✅ Conta Ativada | ID: {UsuarioId} | Email: {Email}",
            evento?.UsuarioId,
            evento?.Email
        );

        // Aqui você pode:
        // - Enviar email de confirmação
        // - Provisionar recursos do usuário
        // - Notificar sistema de gamificação
    }

    private void ProcessarSenhaAlterada(string json)
    {
        var evento = JsonSerializer.Deserialize<SenhaAlteradaEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _logger.LogInformation(
            "🔐 Senha Alterada | ID: {UsuarioId} | Email: {Email}",
            evento?.UsuarioId,
            evento?.Email
        );

        // Aqui você pode:
        // - Enviar email de notificação de alteração de senha
        // - Invalidar sessões antigas
        // - Registrar no log de auditoria
    }

    private void ProcessarPerfilAlterado(string json)
    {
        var evento = JsonSerializer.Deserialize<PerfilAlteradoEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _logger.LogInformation(
            "👤 Perfil Alterado | ID: {UsuarioId} | {PerfilAntigo} → {PerfilNovo}",
            evento?.UsuarioId,
            evento?.PerfilAntigo,
            evento?.PerfilNovo
        );

        // Aqui você pode:
        // - Notificar o usuário
        // - Atualizar permissões em cache
        // - Registrar alteração no sistema de auditoria
    }
}

/// <summary>
/// Exemplo de consumer específico para serviço de notificações
/// Este consumer escuta apenas eventos relevantes para envio de emails
/// </summary>
public class NotificationsConsumerExemplo
{
    public void IniciarConsumer()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection("Notifications-Service");
        using var channel = connection.CreateModel();

        // Fila dedicada para o serviço de notificações
        channel.QueueDeclare(
            queue: "notifications.users",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        // Bind apenas para eventos que requerem notificação
        channel.QueueBind("notifications.users", "fcg.users.events", "users.created");
        channel.QueueBind("notifications.users", "fcg.users.events", "users.activated");
        channel.QueueBind("notifications.users", "fcg.users.events", "users.password.changed");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"🔔 Notificação disparada para: {ea.RoutingKey}");

            // Simula envio de email
            await Task.Delay(500);
            Console.WriteLine("✉️ Email enviado com sucesso!");

            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume("notifications.users", false, consumer);

        Console.WriteLine("Notifications Service ativo. Pressione ENTER para sair...");
        Console.ReadLine();
    }
}

/// <summary>
/// Exemplo de consumer para serviço de auditoria
/// Este consumer registra TODOS os eventos para compliance
/// </summary>
public class AuditConsumerExemplo
{
    public void IniciarConsumer()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection("Audit-Service");
        using var channel = connection.CreateModel();

        // Fila dedicada para auditoria
        channel.QueueDeclare(
            queue: "audit.users.all",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        // Bind para TODOS os eventos (wildcard)
        channel.QueueBind("audit.users.all", "fcg.users.events", "users.*");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Persiste no banco de auditoria
            Console.WriteLine($"📝 [AUDIT] {ea.RoutingKey} | {DateTime.UtcNow:O}");
            Console.WriteLine($"   Dados: {message}");

            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume("audit.users.all", false, consumer);

        Console.WriteLine("Audit Service ativo. Pressione ENTER para sair...");
        Console.ReadLine();
    }
}
