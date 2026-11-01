namespace fcg_users_api.Events.DomainEvents;

/// <summary>
/// Interface base para todos os eventos de domínio
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Identificador único do evento
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp de quando o evento ocorreu
    /// </summary>
    DateTime OcorridoEm { get; }

    /// <summary>
    /// Tipo do evento (nome da classe)
    /// </summary>
    string EventType { get; }
}
