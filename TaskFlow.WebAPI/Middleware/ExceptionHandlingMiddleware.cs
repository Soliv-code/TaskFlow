using System.Data.Common;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

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
                // 1. Берем папку для ошибок из конфига (по умолчанию "Logs/Errors")
                var errorLogDirectory = _configuration["LoggingConfig:ErrorLogDirectory"] ?? "Logs/Errors";

                // 2. Формируем имя файла с датой и временем
                var fileName = $"taskflow-errors_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                var logFullPath = Path.Combine(_env.ContentRootPath, errorLogDirectory, fileName);

                // 3. Создаем папку, если её нет
                var logDir = Path.GetDirectoryName(logFullPath);
                if (!string.IsNullOrEmpty(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // 4. Формируем читаемую запись с длинным разделителем (80 символов)
                var separator = new string('-', 80);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB Connection Error:\n{exception}\n{separator}\n\n";

                // 5. Дописываем в файл
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
        // Обработка бизнес-исключений (400 Bad Request)
        if (exception is TaskFlow.Application.Exceptions.BusinessException businessEx)
        {
            _logger.LogWarning("⚠️ Бизнес-ошибка: {Message}", businessEx.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "Ошибка валидации",
                detail = businessEx.Message,
                status = 400
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