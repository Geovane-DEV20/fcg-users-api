using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using fcg_users_api.DTOs;
using fcg_users_api.Middlewares;
using fcg_users_api.Services;

namespace fcg_users_api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza login e retorna um token JWT.
    /// </summary>
    /// <param name="request">Credenciais de login (email e senha)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token JWT e informações do usuário</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Obtém informações do usuário atualmente autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do usuário logado</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterUsuarioAtual(CancellationToken cancellationToken)
    {
        var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await _authService.ObterUsuarioAtualAsync(usuarioId, cancellationToken);
        return Ok(usuario);
    }

    /// <summary>
    /// Realiza logout (placeholder - JWT é stateless).
    /// </summary>
    /// <returns>Confirmação de logout</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        // JWT é stateless, então o logout é feito no cliente (descartando o token)
        // Aqui podemos adicionar o token a uma blacklist se necessário
        
        _logger.LogInformation("Usuário {UsuarioId} fez logout", User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        return NoContent();
    }
}
