using TaskFlow.Application.Contracts.Authentication;

namespace TaskFlow.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(int userId, string refreshToken);
}
