
using Microsoft.Extensions.Logging;

public delegate void LogMessage(LogLevel logLevel, string message);

public class MauiLogger : ILogger
{
    public event LogMessage? OnLogMessage;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        OnLogMessage?.Invoke(logLevel, formatter(state, exception));
    }
}
