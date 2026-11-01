using fcg_users_api.DTOs;
using fcg_users_api.Exceptions;
using fcg_users_api.Models;
using fcg_users_api.Models.Enums;
using fcg_users_api.Models.ValueObjects;
using fcg_users_api.Repositories;

namespace fcg_users_api.Services;

public class CadastroService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<CadastroService> _logger;

    public CadastroService(IUsuarioRepository usuarioRepository, ILogger<CadastroService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    public async Task<CadastrarUsuarioResponse> CadastrarUsuarioAsync(CadastrarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        var email = new Email(request.Email);

        if (await _usuarioRepository.ExisteComEmailAsync(email, cancellationToken))
            throw new RecursoDuplicadoException("EMAIL_DUPLICADO_DETECTADO", "Já existe uma conta cadastrada com este e-mail.");

        var usuario = Usuario.CadastrarConta(request.Nome, email, request.Senha, DateTime.UtcNow);

        await _usuarioRepository.AdicionarAsync(usuario, cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Usuário cadastrado com sucesso. ID: {UsuarioId}, Email: {Email}", usuario.Id, email.Valor);

        // TODO: Enviar email de ativação com o token: usuario.TokenAtivacao
        // Isso pode ser feito via mensageria (RabbitMQ, Azure Service Bus, etc.) em um microsserviço

        return new CadastrarUsuarioResponse(usuario.Id);
    }

    public async Task ConfirmarEmailAsync(ConfirmarEmailRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorTokenDeAtivacaoAsync(request.Token, cancellationToken)
            ?? throw new DomainException("TOKEN_ATIVACAO_INVALIDO", "O token de ativação informado é inválido.");

        usuario.ConfirmarEmail(request.Token, DateTime.UtcNow);

        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Email confirmado com sucesso. UsuarioId: {UsuarioId}", usuario.Id);

        // TODO: Provisionar biblioteca do usuário ou outros recursos iniciais
        // Isso pode disparar um evento para outros microsserviços
    }

    public async Task<UsuarioResponse> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        return UsuarioResponse.DeUsuario(usuario);
    }

    public async Task<UsuarioResponse> AtualizarDadosAsync(Guid id, AtualizarDadosRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        usuario.AtualizarDados(request.Nome, DateTime.UtcNow);

        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Dados do usuário atualizados. UsuarioId: {UsuarioId}", usuario.Id);

        return UsuarioResponse.DeUsuario(usuario);
    }

    public async Task AlterarSenhaAsync(Guid id, AlterarSenhaRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        usuario.AlterarSenha(request.SenhaAtual, request.NovaSenha, DateTime.UtcNow);

        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Senha alterada com sucesso. UsuarioId: {UsuarioId}", usuario.Id);
    }

    public async Task<UsuarioResponse> AlterarPerfilAsync(Guid id, AlterarPerfilRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        // Validar perfil
        if (!Enum.TryParse<Perfil>(request.Perfil, ignoreCase: true, out var novoPerfil))
            throw new DomainException("PERFIL_INVALIDO", "Perfil inválido. Use 'Usuario' ou 'Administrador'.");

        usuario.AlterarPerfil(novoPerfil, DateTime.UtcNow);

        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Perfil alterado para {Perfil}. UsuarioId: {UsuarioId}", novoPerfil, usuario.Id);

        // TODO: Publicar evento PerfilAlteradoEvent para outros serviços

        return UsuarioResponse.DeUsuario(usuario);
    }

    public async Task<(IReadOnlyList<UsuarioResponse> Usuarios, int Total)> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken = default)
    {
        var usuarios = await _usuarioRepository.ListarAsync(pagina, tamanhoPagina, cancellationToken);
        var total = await _usuarioRepository.ContarAsync(cancellationToken);

        var usuariosResponse = usuarios.Select(UsuarioResponse.DeUsuario).ToList();

        return (usuariosResponse, total);
    }
}
