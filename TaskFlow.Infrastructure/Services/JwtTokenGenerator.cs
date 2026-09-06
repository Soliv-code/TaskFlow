using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Settings;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private JwtSettings _jwtSettings;
    // Внедряем настройки через IOptions (стандартный паттерн .NET)
    public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }
    public string GenerateToken(User user)
    {
        // 1. Создаем ключ подписи на основе нашего секрета из appsettings
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        // 2. Формируем Claims (полезную нагрузку токена)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 3. Создаем сам токен
        var securityToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            claims: claims,
            signingCredentials: signingCredentials);

        // 4. Возвращаем токен в виде строки
        return new JwtSecurityTokenHandler().WriteToken(securityToken);

    }
}
