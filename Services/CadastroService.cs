using fcg_users_api.DTOs;
using fcg_users_api.Events;
using fcg_users_api.Events.DomainEvents;
using fcg_users_api.Exceptions;
using fcg_users_api.Models;
using fcg_users_api.Models.Enums;
using fcg_users_api.Models.ValueObjects;
using fcg_users_api.Repositories;

namespace fcg_users_api.Services;

public class CadastroService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CadastroService> _logger;

    public CadastroService(
        IUsuarioRepository usuarioRepository,
        IEventPublisher eventPublisher,
        ILogger<CadastroService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _eventPublisher = eventPublisher;
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

        // Publica evento de domínio
        try
        {
            var evento = new UsuarioCriadoEvent(
                usuario.Id,
                usuario.Nome,
                email.Valor,
                usuario.Perfil.ToString()
            );
            await _eventPublisher.PublicarAsync(evento, cancellationToken);
            _logger.LogInformation("Evento UsuarioCriado publicado com sucesso. UsuarioId: {UsuarioId}", usuario.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento UsuarioCriado para usuário {UsuarioId}", usuario.Id);
            // Não falha a operação se a publicação do evento falhar (eventual consistency)
        }

        // TODO: Enviar email de ativação com o token: usuario.TokenAtivacao
        // Isso será feito por um microsserviço de notificações que escuta o evento UsuarioCriado

        return new CadastrarUsuarioResponse(usuario.Id);
    }

    public async Task ConfirmarEmailAsync(ConfirmarEmailRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorTokenDeAtivacaoAsync(request.Token, cancellationToken)
            ?? throw new DomainException("TOKEN_ATIVACAO_INVALIDO", "O token de ativação informado é inválido.");

        usuario.ConfirmarEmail(request.Token, DateTime.UtcNow);

        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Email confirmado com sucesso. UsuarioId: {UsuarioId}", usuario.Id);

        // Publica evento de domínio
        try
        {
            var evento = new ContaAtivadaEvent(usuario.Id, usuario.Email.Valor);
            await _eventPublisher.PublicarAsync(evento, cancellationToken);
            _logger.LogInformation("Evento ContaAtivada publicado com sucesso. UsuarioId: {UsuarioId}", usuario.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento ContaAtivada para usuário {UsuarioId}", usuario.Id);
        }

        // Provisionar biblioteca do usuário ou outros recursos iniciais
        // Isso será feito por outros microsserviços que escutam o evento ContaAtivada
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

        // Publica evento de domínio
        try
        {
            var evento = new SenhaAlteradaEvent(usuario.Id, usuario.Email.Valor);
            await _eventPublisher.PublicarAsync(evento, cancellationToken);
            _logger.LogInformation("Evento SenhaAlterada publicado com sucesso. UsuarioId: {UsuarioId}", usuario.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento SenhaAlterada para usuário {UsuarioId}", usuario.Id);
        }
    }

    public async Task<UsuarioResponse> AlterarPerfilAsync(Guid id, AlterarPerfilRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("USUARIO_NAO_ENCONTRADO", "Usuário não encontrado.");

        // Validar perfil
        if (!Enum.TryParse<Perfil>(request.Perfil, ignoreCase: true, out var novoPerfil))
            throw new DomainException("PERFIL_INVALIDO", "Perfil inválido. Use 'Usuario' ou 'Administrador'.");

        var perfilAntigo = usuario.Perfil;
        usuario.AlterarPerfil(novoPerfil, DateTime.UtcNow);

        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Perfil alterado para {Perfil}. UsuarioId: {UsuarioId}", novoPerfil, usuario.Id);

        // Publica evento de domínio
        try
        {
            var evento = new PerfilAlteradoEvent(
                usuario.Id,
                usuario.Email.Valor,
                perfilAntigo.ToString(),
                novoPerfil.ToString(),
                "Sistema" // TODO: Extrair do contexto de autenticação
            );
            await _eventPublisher.PublicarAsync(evento, cancellationToken);
            _logger.LogInformation("Evento PerfilAlterado publicado com sucesso. UsuarioId: {UsuarioId}", usuario.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento PerfilAlterado para usuário {UsuarioId}", usuario.Id);
        }

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
