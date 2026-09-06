using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace TaskFlow.WebAPI.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate _next,
    ILogger<ExceptionHandlingMiddleware> _logger,
    IConfiguration _configuration,
    IWebHostEnvironment _env) // <-- Добавили среду хостинга для получения корня проекта
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
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

            // Запись в файл на основе конфигурации (безопасно, вне папки bin!)
            try
            {
                // 1. Читаем относительный путь из appsettings.json (с фоллбэком по умолчанию)
                var logRelativePath = _configuration["LoggingConfig:ErrorLogPath"] ?? "Logs/taskflow-errors.log";

                // 2. Собираем абсолютный путь относительно корня проекта (ContentRootPath)
                var logFullPath = Path.Combine(_env.ContentRootPath, logRelativePath);

                // 3. Создаем директорию, если её нет
                var logDir = Path.GetDirectoryName(logFullPath);
                if (!string.IsNullOrEmpty(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // 4. Дописываем ошибку в конец файла
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB Connection Error:\n{exception}\n{'-',50}\n";
                File.AppendAllText(logFullPath, logEntry);
            }
            catch (Exception fileEx)
            {
                // Если вдруг нет прав на запись, мы хотя бы не уроним приложение, а просто залогим это
                _logger.LogError(fileEx, "Не удалось записать ошибку в лог-файл по пути {Path}", _configuration["LoggingConfig:ErrorLogPath"]);
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
        _logger.LogError(exception, "⚠ Необработанное исключение: {Message}", exception.Message);

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