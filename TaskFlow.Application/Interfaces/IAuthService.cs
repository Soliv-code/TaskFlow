using TaskFlow.Application.Contracts.Authentication;
using TaskFlow.Application.Contracts.Users;

namespace TaskFlow.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(int userId, string refreshToken);
    // Создание пользователя (только для админа)
    Task<bool> CreateUserAsync(CreateUserRequest request);
    Task<List<int>> GetExpiredTokenIdsAsync(int? userId = null);
    Task<List<int>> DeleteExpiredTokensAsync(int? userId = null);
}
