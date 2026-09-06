using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace TaskFlow.WebAPI.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate _next,
    ILogger<ExceptionHandlingMiddleware> _logger,
    IConfiguration _configuration,
    IWebHostEnvironment _env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        bool isDbConnectionError = exception is DbException ||
                                   exception is SocketException ||
                                   (exception is InvalidOperationException && exception.InnerException is SocketException) ||
                                   exception.Message.Contains("Подключение не установлено", StringComparison.OrdinalIgnoreCase) ||
                                   exception.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                                   exception.Message.Contains("failed to connect", StringComparison.OrdinalIgnoreCase) ||
                                   (exception is InvalidOperationException inv && inv.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase));

        if (isDbConnectionError)
        {
            _logger.LogWarning("⚠️ Ошибка подключения к БД: {Message}. Возможно, не запущен Docker-контейнер.", exception.Message);

            try
            {
                var logRelativePath = _configuration["LoggingConfig:ErrorLogPath"] ?? "Logs/taskflow-errors.log";
                var logFullPath = Path.Combine(_env.ContentRootPath, logRelativePath);
                var logDir = Path.GetDirectoryName(logFullPath);

                if (!string.IsNullOrEmpty(logDir)) Directory.CreateDirectory(logDir);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB Connection Error:\n{exception}\n{'-',50}\n";
                File.AppendAllText(logFullPath, logEntry);
            }
            catch (Exception fileEx)
            {
                _logger.LogError(fileEx, "Не удалось записать ошибку в лог-файл");
            }

            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "База данных недоступна",
                detail = "Не удалось подключиться к PostgreSQL. Убедитесь, что Docker-контейнер 'taskflow-db' запущен (команда: docker ps).",
                status = 503
            }));
            return;
        }

        _logger.LogError(exception, "⚠ Необработанное исключение: {Message}", exception.Message);
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            title = "Внутренняя ошибка сервера",
            detail = "Произошла непредвиденная ошибка. Проверьте логи.",
            status = 500
        }));
    }
}