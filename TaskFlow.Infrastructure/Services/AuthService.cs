using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Authentication;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Settings;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Context;
using Microsoft.Extensions.Options;

namespace TaskFlow.Infrastructure.Services;

public class AuthService(
    AppDbContext _context,
    IPasswordHasher _passwordHasher,
    IJwtTokenGenerator _jwtTokenGenerator,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // 1. Ищем пользователя по логину
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user is null) return null;

        // 2. Проверяем пароль
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        // 3. Отзываем ВСЕ старые активные токены этого пользователя
        await RevokeAllUserTokensAsync(user.Id);

        // 4. Генерируем Access Token
        var accessToken = _jwtTokenGenerator.GenerateToken(user);

        // 5. Генерируем и сохраняем Refresh Token
        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);

        // 6. Сохраняем изменения (отзыв старых + добавление нового)
        await _context.SaveChangesAsync();

        // 7. Возвращаем ответ
        return new AuthResponse(
            user.Id, 
            user.Username, 
            user.Email, 
            accessToken, 
            refreshToken
        );
    }

    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // 1. Ищем токен в БД
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        // 2. Проверяем, существует ли токен, не истек ли он и не отозван ли
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return null;

        // 3. Помечаем старый токен как отозванный (ротация)
        storedToken.IsRevoked = true;

        // 4. Генерируем новую пару токенов
        var newAccessToken = _jwtTokenGenerator.GenerateToken(storedToken.User);
        var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(storedToken.UserId);

        await _context.SaveChangesAsync();

        // 5. Возвращаем новую пару
        return new AuthResponse(
            storedToken.User.Id,
            storedToken.User.Username,
            storedToken.User.Email,
            newAccessToken,
            newRefreshToken
        );
    }

    public async Task LogoutAsync(int userId, string refreshToken)
    {
        // При выходе мы просто отзываем ВСЕ активные токены этого пользователя.
        // Переданный refreshToken можно использовать для аудита (логирования), 
        // но с точки зрения безопасности нам важно обнулить всё.
        await RevokeAllUserTokensAsync(userId);
        await _context.SaveChangesAsync();
    }

    // Централизованная очистка всех активных токенов пользователя
    private async Task RevokeAllUserTokensAsync(int userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }
    }

    // Метод для генерации криптографически стойкого Refresh Token
    private async Task<string> GenerateAndSaveRefreshTokenAsync(int userId)
    {
        // Генерируем случайный токен (256 бит = 32 байта, кодируем в Base64)
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        // Создаем сущность RefreshToken
        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        // Сохраняем в БД
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return refreshToken;
    }
}