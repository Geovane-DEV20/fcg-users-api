using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FcgUsersApi.Data;
using FcgUsersApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FcgUsersApi.Services;

public class AuthService : IAuthService
{
    private readonly UsersDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UsersDbContext context, IConfiguration configuration, IRabbitMqService rabbitMqService, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    public async Task<LoginResponse?> Register(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            CriadoEm = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Publicar evento UserCreatedEvent
        _rabbitMqService.PublishMessage("user.created", new
        {
            userId = user.Id,
            nome = user.Nome,
            email = user.Email,
            timestamp = DateTime.UtcNow
        });

        _logger.LogInformation($"Usuário {user.Email} registrado com sucesso. Evento UserCreatedEvent publicado.");

        return new LoginResponse
        {
            Token = GenerateJwtToken(user),
            UserId = user.Id,
            Nome = user.Nome,
            Email = user.Email
        };
    }

    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        return new LoginResponse
        {
            Token = GenerateJwtToken(user),
            UserId = user.Id,
            Nome = user.Nome,
            Email = user.Email
        };
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "your-super-secret-key-change-this-in-production-min-32-chars";
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Nome),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Perfil)
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
