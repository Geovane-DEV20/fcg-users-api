using fcg_users_api.Events.DomainEvents;

namespace fcg_users_api.Events;

/// <summary>
/// Interface para publicação de eventos de domínio
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publica um evento de domínio para o message broker
    /// </summary>
    /// <typeparam name="TEvent">Tipo do evento</typeparam>
    /// <param name="evento">Instância do evento</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task PublicarAsync<TEvent>(TEvent evento, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;

    /// <summary>
    /// Publica múltiplos eventos em batch
    /// </summary>
    /// <param name="eventos">Lista de eventos</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task PublicarBatchAsync(IEnumerable<IDomainEvent> eventos, CancellationToken cancellationToken = default);
}
