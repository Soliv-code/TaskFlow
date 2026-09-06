using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace TaskFlow.WebAPI.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IConfiguration configuration,
    IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Проверяем, является ли ошибка проблемой подключения к БД
        bool isDbConnectionError = exception is DbException ||
                                   exception is SocketException ||
                                   (exception is InvalidOperationException && exception.InnerException is SocketException) ||
                                   exception.Message.Contains("Подключение не установлено", StringComparison.OrdinalIgnoreCase) ||
                                   exception.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                                   exception.Message.Contains("failed to connect", StringComparison.OrdinalIgnoreCase) ||
                                   (exception is InvalidOperationException inv && inv.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase));

        if (isDbConnectionError)
        {
            logger.LogWarning("⚠️ Ошибка подключения к БД: {Message}. Возможно, не запущен Docker-контейнер.", exception.Message);

            // 🛡️ Запись полного стектрейса в файл (0 сторонних зависимостей!)
            try
            {
                var logRelativePath = configuration["LoggingConfig:ErrorLogPath"] ?? "Logs/taskflow-errors.log";
                var logFullPath = Path.Combine(env.ContentRootPath, logRelativePath);
                var logDir = Path.GetDirectoryName(logFullPath);

                if (!string.IsNullOrEmpty(logDir)) Directory.CreateDirectory(logDir);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB Connection Error:\n{exception}\n{'-',50}\n";
                File.AppendAllText(logFullPath, logEntry);
            }
            catch (Exception fileEx)
            {
                logger.LogError(fileEx, "Не удалось записать ошибку в лог-файл по пути {Path}", configuration["LoggingConfig:ErrorLogPath"]);
            }

            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable; // 503
            context.Response.ContentType = "application/json";

            var response = new
            {
                title = "База данных недоступна",
                detail = "Не удалось подключиться к PostgreSQL. Убедитесь, что Docker-контейнер 'taskflow-db' запущен (команда: docker ps).",
                status = 503
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            return;
        }

        // Для всех остальных непредвиденных ошибок
        logger.LogError(exception, "⚠ Необработанное исключение: {Message}", exception.Message);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; // 500
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            title = "Внутренняя ошибка сервера",
            detail = "Произошла непредвиденная ошибка. Проверьте логи.",
            status = 500
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}