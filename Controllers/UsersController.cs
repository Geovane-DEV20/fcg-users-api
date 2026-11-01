using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using fcg_users_api.DTOs;
using fcg_users_api.Services;
using fcg_users_api.Middlewares;

namespace fcg_users_api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly CadastroService _cadastroService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(CadastroService cadastroService, ILogger<UsersController> logger)
    {
        _cadastroService = cadastroService;
        _logger = logger;
    }

    /// <summary>
    /// Cadastra uma nova conta de usuário. [Público - não requer autenticação]
    /// </summary>
    /// <param name="request">Dados do usuário a ser cadastrado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>ID do usuário cadastrado</returns>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CadastrarUsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar(CadastrarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var resposta = await _cadastroService.CadastrarUsuarioAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = resposta.Id }, resposta);
    }

    /// <summary>
    /// Confirma o e-mail do usuário através do token de ativação. [Público - não requer autenticação]
    /// </summary>
    /// <param name="request">Token de ativação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPost("ativacao")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status410Gone)]
    public async Task<IActionResult> ConfirmarEmail(ConfirmarEmailRequest request, CancellationToken cancellationToken)
    {
        await _cadastroService.ConfirmarEmailAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Obtém um usuário pelo ID. [Requer autenticação]
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var resposta = await _cadastroService.ObterPorIdAsync(id, cancellationToken);
        return Ok(resposta);
    }

    /// <summary>
    /// Atualiza os dados cadastrais do usuário. [Requer autenticação]
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="request">Novos dados do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarDados(Guid id, AtualizarDadosRequest request, CancellationToken cancellationToken)
    {
        var resposta = await _cadastroService.AtualizarDadosAsync(id, request, cancellationToken);
        return Ok(resposta);
    }

    /// <summary>
    /// Altera a senha do usuário. [Requer autenticação]
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="request">Senha atual e nova senha</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPut("{id:guid}/senha")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarSenha(Guid id, AlterarSenhaRequest request, CancellationToken cancellationToken)
    {
        await _cadastroService.AlterarSenhaAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Altera o perfil do usuário (Usuario ou Administrador). [Requer autenticação - APENAS ADMINISTRADORES]
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="request">Novo perfil</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpPatch("{id:guid}/perfil")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarPerfil(Guid id, AlterarPerfilRequest request, CancellationToken cancellationToken)
    {
        var resposta = await _cadastroService.AlterarPerfilAsync(id, request, cancellationToken);
        return Ok(resposta);
    }

    /// <summary>
    /// Lista todos os usuários com paginação. [Requer autenticação - APENAS ADMINISTRADORES]
    /// </summary>
    /// <param name="pagina">Número da página</param>
    /// <param name="tamanhoPagina">Tamanho da página</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 20, CancellationToken cancellationToken = default)
    {
        var (usuarios, total) = await _cadastroService.ListarAsync(pagina, tamanhoPagina, cancellationToken);
        
        return Ok(new 
        { 
            usuarios, 
            total, 
            pagina, 
            tamanhoPagina,
            totalPaginas = (int)Math.Ceiling(total / (double)tamanhoPagina)
        });
    }
}
