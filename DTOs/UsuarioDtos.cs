using fcg_users_api.Models;

namespace fcg_users_api.DTOs;

public sealed record CadastrarUsuarioRequest(string Nome, string Email, string Senha);

public sealed record CadastrarUsuarioResponse(Guid Id);

public sealed record ConfirmarEmailRequest(string Token);

public sealed record LoginRequest(string Email, string Senha);

public sealed record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public UsuarioResponse Usuario { get; init; } = null!;
}

public sealed record AtualizarDadosRequest(string Nome);

public sealed record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);

public sealed record AlterarPerfilRequest(string Perfil); // "Usuario" ou "Administrador"

public sealed record UsuarioResponse(
    Guid Id, 
    string Nome, 
    string Email, 
    string Perfil, 
    string Status, 
    DateTime CriadoEm)
{
    public static UsuarioResponse DeUsuario(Usuario usuario) => new(
        usuario.Id,
        usuario.Nome,
        usuario.Email.Valor,
        usuario.Perfil.ToString(),
        usuario.Status.ToString(),
        usuario.CriadoEm);
}
