using TaskFlow.Application.Contracts.Authentication;

namespace TaskFlow.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
