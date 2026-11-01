namespace fcg_users_api.Events.DomainEvents;

/// <summary>
/// Evento de domínio disparado quando um novo usuário é criado
/// </summary>
public class UsuarioCriadoEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OcorridoEm { get; }
    public string EventType => "UsuarioCriado";

    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }

    public UsuarioCriadoEvent()
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
    }

    public UsuarioCriadoEvent(Guid usuarioId, string nome, string email, string perfil)
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
        UsuarioId = usuarioId;
        Nome = nome;
        Email = email;
        Perfil = perfil;
        CriadoEm = DateTime.UtcNow;
    }
}
