using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace fcg_users_api.Events.RabbitMQ;

/// <summary>
/// Health check para verificar conexão com RabbitMQ
/// </summary>
public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQHealthCheck> _logger;

    public RabbitMQHealthCheck(
        IConfiguration configuration,
        ILogger<RabbitMQHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            if (connection.IsOpen && channel.IsOpen)
            {
                return Task.FromResult(
                    HealthCheckResult.Healthy("RabbitMQ está acessível e funcionando")
                );
            }

            return Task.FromResult(
                HealthCheckResult.Unhealthy("RabbitMQ está inacessível")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no health check do RabbitMQ");
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Erro ao conectar ao RabbitMQ",
                    ex
                )
            );
        }
    }
}
