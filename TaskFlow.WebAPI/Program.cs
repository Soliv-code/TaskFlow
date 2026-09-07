using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Settings;
using TaskFlow.Infrastructure.Context;
using TaskFlow.Infrastructure.Services;
using TaskFlow.WebAPI.Middleware;

// Нужно для отображения emoji в консоли
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// TODO: Выводить чистый лог, а не эту EF поебень с sensitive information

// Регистрируем DbContext с параметром подключения из appsettings.json
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           //.EnableSensitiveDataLogging() // Показывает значения параметров в логах
           //.LogTo(Console.WriteLine, LogLevel.Information) // Выводит SQL в консоль
);


// Регистрация настроек JWT из appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Регистрация генератора токенов
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Регистрация проверки совпадает ли введенный пароль с хешем из БД
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Регистрация проверок:
// * Ищем пользователя;
// * Проверяем пароль;
// * Генерируем токен
builder.Services.AddScoped<IAuthService, AuthService>();

// Получаем секретный ключ с гарантией, что он не null
var secretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT Secret Key is not configured in appsettings.json");

// Настройка аутентификации (JWT Bearer)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

var app = builder.Build();
// Глобальная обработка исключений
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
