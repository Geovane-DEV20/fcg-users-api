using fcg_users_api.DTOs;
using fcg_users_api.Exceptions;
using fcg_users_api.Models.Enums;
using fcg_users_api.Models.ValueObjects;
using fcg_users_api.Repositories;

namespace fcg_users_api.Services;

public class AuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        TokenService tokenService,
        ILogger<AuthService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = new Email(request.Email);
        
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken)
            ?? throw new NaoAutorizadoException("AUTENTICACAO_RECUSADA", "E-mail ou senha inválidos.");

        // Validar senha e registrar tentativas
        usuario.Autenticar(request.Senha, DateTime.UtcNow);

        // Se chegou aqui, autenticação foi bem-sucedida
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        // Verificar se a conta está ativa
        if (usuario.Status != StatusConta.Ativa)
        {
            throw new RegraDeNegocioException("CONTA_INATIVA", 
                $"Conta com status '{usuario.Status}'. Apenas contas ativas podem fazer login.");
        }

        // Gerar token JWT
        var token = _tokenService.GerarToken(usuario);

        _logger.LogInformation("Login bem-sucedido para usuário {UsuarioId} - {Email}", usuario.Id, usuario.Email.Valor);

        return new LoginResponse
        {
            Token = token,
            ExpiresIn = 3600, // 1 hora (configurável)
            Usuario = UsuarioResponse.DeUsuario(usuario)
        };
    }

    public async Task<UsuarioResponse> ObterUsuarioAtualAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        return UsuarioResponse.DeUsuario(usuario);
    }
}
