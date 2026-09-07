using Microsoft.Extensions.Logging;
using System.IO;

namespace TaskFlow.WebAPI.Logging;

// 1. Провайдер, который создает логгеры
public class NativeFileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private static readonly object _lock = new object(); // Для потокобезопасности

    public NativeFileLoggerProvider(string filePath) => _filePath = filePath;

    public ILogger CreateLogger(string categoryName) => new NativeFileLogger(_filePath, categoryName);
    public void Dispose() { }
}

// 2. Сам логгер, который пишет в файл
public class NativeFileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _category;
    private static readonly object _lock = new object();

    public NativeFileLogger(string filePath, string category)
    {
        _filePath = filePath;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null) return;

        // Блокируем поток, чтобы несколько одновременных запросов не испортили файл
        lock (_lock)
        {
            try
            {
                var logDir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(logDir)) Directory.CreateDirectory(logDir);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel,-13}] [{_category}]\n      {message}\n";
                File.AppendAllText(_filePath, logEntry);

                if (exception != null)
                {
                    File.AppendAllText(_filePath, $"      {exception}\n");
                }

                // 🌟 ДОБАВЛЯЕМ ПУСТУЮ СТРОКУ ДЛЯ РАЗДЕЛЕНИЯ ЗАПИСЕЙ
                File.AppendAllText(_filePath, "\n");
            }
            catch
            {
                // Если не удалось записать в файл, мы не должны ронять всё приложение
            }
        }
    }
}