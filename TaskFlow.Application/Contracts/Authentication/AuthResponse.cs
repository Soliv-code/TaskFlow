namespace TaskFlow.Application.Contracts.Authentication;

public record AuthResponse(
    int Id,
    string Username,
    string Email,
    string Token,
    string RefreshToken,
    bool MustChangePassword // Флаг для фронтенда
);
