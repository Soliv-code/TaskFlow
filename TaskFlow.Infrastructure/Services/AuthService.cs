using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Authentication;
using TaskFlow.Application.Interfaces;
using TaskFlow.Infrastructure.Context;

namespace TaskFlow.Infrastructure.Services;

public class AuthService(
    AppDbContext _context, 
    IPasswordHasher _passwordHasher, 
    IJwtTokenGenerator _jwtTokenGenerator) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // 1. Ищем пользователя по логину
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null) return null;

        // 2. Проверяем пароль
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        // 3. Генерируем токен
        var token = _jwtTokenGenerator.GenerateToken(user);

        // 4. Возвращаем ответ
        return new AuthResponse(
            user.Id, 
            user.Username, 
            user.Email, 
            token
        );
    }
}
