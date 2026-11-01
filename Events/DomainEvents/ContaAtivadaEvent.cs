namespace fcg_users_api.Events.DomainEvents;

/// <summary>
/// Evento de domínio disparado quando uma conta de usuário é ativada
/// </summary>
public class ContaAtivadaEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OcorridoEm { get; }
    public string EventType => "ContaAtivada";

    public Guid UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime AtivadaEm { get; set; }

    public ContaAtivadaEvent()
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
    }

    public ContaAtivadaEvent(Guid usuarioId, string email)
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
        UsuarioId = usuarioId;
        Email = email;
        AtivadaEm = DateTime.UtcNow;
    }
}
