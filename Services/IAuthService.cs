using FcgUsersApi.Models;

namespace FcgUsersApi.Services;

public interface IAuthService
{
    Task<LoginResponse?> Register(RegisterRequest request);
    Task<LoginResponse?> Login(LoginRequest request);
    string GenerateJwtToken(User user);
}
