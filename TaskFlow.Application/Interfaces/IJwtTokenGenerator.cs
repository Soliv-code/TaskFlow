using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces;

public interface IJwtTokenGenerator
{
    // Принимает пользователя и возвращает готовую строку JWT-токена
    string GenerateToken(User user);
}
