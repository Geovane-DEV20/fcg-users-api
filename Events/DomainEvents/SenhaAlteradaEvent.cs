namespace fcg_users_api.Events.DomainEvents;

/// <summary>
/// Evento de domínio disparado quando a senha de um usuário é alterada
/// </summary>
public class SenhaAlteradaEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OcorridoEm { get; }
    public string EventType => "SenhaAlterada";

    public Guid UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime AlteradaEm { get; set; }

    public SenhaAlteradaEvent()
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
    }

    public SenhaAlteradaEvent(Guid usuarioId, string email)
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
        UsuarioId = usuarioId;
        Email = email;
        AlteradaEm = DateTime.UtcNow;
    }
}
