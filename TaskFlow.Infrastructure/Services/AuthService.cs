using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using TaskFlow.Application.Contracts.Authentication;
using TaskFlow.Application.Contracts.Users;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Settings;
using TaskFlow.Application.Validators;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Context;

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
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

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
            refreshToken,
            user.MustChangePassword
        );
    }

    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // 1. Ищем токен в БД и загружаем связанные данные (User и его Role)
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Role)
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

        // 5. Возвращаем новую пару с актуальным флагом MustChangePassword
        return new AuthResponse(
            storedToken.User.Id,
            storedToken.User.Username,
            storedToken.User.Email,
            newAccessToken,
            newRefreshToken,
            storedToken.User.MustChangePassword // ДОБАВЛЕНО: 6-й параметр
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

    public async Task<bool> CreateUserAsync(CreateUserRequest request)
    {
        // 1. Валидация сложности пароля (используем наш новый валидатор)
        PasswordValidator.Validate(request.Password);

        // 2. Проверка уникальности с понятными ошибками (вместо молчаливого return false)
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            throw new BusinessException("Пользователь с таким именем уже существует");

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new BusinessException("Пользователь с таким email уже существует");

        // 3. Ищем роль в БД по имени (например, "Admin" или "User")
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            throw new BusinessException($"Роль '{request.Role}' не найдена в системе. Доступны: Admin, User");

        // 4. Создаём сущность пользователя
        var newUser = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            RoleId = role.Id,                 // ИСПРАВЛЕНО: присваиваем числовой ID роли
            MustChangePassword = true         // НОВОЕ: пароль считается временным, требует смены
        };

        // 5. Сохраняем с дополнительной защитой от UNIQUE constraint (на всякий случай)
        try
        {
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true || ex.InnerException?.Message.Contains("unique") == true)
        {
            // 23505 - это код ошибки PostgreSQL для нарушения уникальности (unique violation)
            throw new BusinessException("Нарушение уникальности: имя пользователя или email уже заняты.");
        }

        return true;
    }

    public async Task<List<int>> GetExpiredTokenIdsAsync(int? userId = null)
    {
        var query = _context.RefreshTokens.Where(rt => rt.ExpiresAt < DateTime.UtcNow);
        if (userId.HasValue)
            query = query.Where(rt => rt.UserId == userId.Value);
        return await query.Select(rt => rt.Id).ToListAsync();
    }

    public async Task<List<int>> DeleteExpiredTokensAsync(int? userId = null)
    {
        var query = _context.RefreshTokens.Where(rt => rt.ExpiresAt < DateTime.UtcNow);

        if (userId.HasValue)
            query = query.Where(rt => rt.UserId == userId.Value);


        var expiredTokens = await query.ToListAsync();

        if (expiredTokens.Count > 0)
        {
            var deletedIds = expiredTokens.Select(rt => rt.Id).ToList();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            return deletedIds;
        }
        return new List<int>();
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        // 1. Базовая проверка совпадения:
        if(request.NewPassword != request.ConfirmPassword)
            throw new BusinessException("Новый пароль и подтверждение не совпадают");

        // 2. Проверяем сложность нового пароля нашим валидатором
        PasswordValidator.Validate(request.NewPassword);

        // 3. Ищем пользователя
        var user = await _context.Users.FindAsync(userId);
        if(user is null)
            throw new BusinessException("Пользователь не найден");

        // 4. Проверяем старый пароль
        if (!_passwordHasher.Verify(request.OldPassword, user.PasswordHash))
            throw new BusinessException("Текущий пароль введен неверно");

        // 5. Проверяем, что пароли разные
        if (request.OldPassword == request.NewPassword)
            throw new BusinessException("Новый пароль должен отличаться от старого");

        // 6. Обновляем хеш и сбрасываем флаг обязательной смены
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}