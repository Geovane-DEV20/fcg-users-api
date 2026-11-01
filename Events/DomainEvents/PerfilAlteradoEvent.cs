namespace fcg_users_api.Events.DomainEvents;

/// <summary>
/// Evento de domínio disparado quando o perfil de um usuário é alterado
/// </summary>
public class PerfilAlteradoEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OcorridoEm { get; }
    public string EventType => "PerfilAlterado";

    public Guid UsuarioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PerfilAntigo { get; set; } = string.Empty;
    public string PerfilNovo { get; set; } = string.Empty;
    public string AlteradoPor { get; set; } = string.Empty;

    public PerfilAlteradoEvent()
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
    }

    public PerfilAlteradoEvent(Guid usuarioId, string email, string perfilAntigo, string perfilNovo, string alteradoPor)
    {
        EventId = Guid.NewGuid();
        OcorridoEm = DateTime.UtcNow;
        UsuarioId = usuarioId;
        Email = email;
        PerfilAntigo = perfilAntigo;
        PerfilNovo = perfilNovo;
        AlteradoPor = alteradoPor;
    }
}
