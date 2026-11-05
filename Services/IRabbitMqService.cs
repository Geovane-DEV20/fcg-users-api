namespace FcgUsersApi.Services;

public interface IRabbitMqService
{
    void PublishMessage(string routingKey, object message);
}
