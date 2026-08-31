

public abstract class BaseLogger : ITP3Logger
{
    public void LogDebug(string message, params object[] args)
    {
        this.Log(LogLevel.Debug, message, args);
    }

    protected abstract void Log(LogLevel logLevel, string message, object[] args);

    public void LogError(string message, params object[] args)
    {
        this.Log(LogLevel.Error, message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        this.Log(LogLevel.Error, $"{message} Exception: {exception}", args);
    }

    public void LogInformation(string message, params object[] args)
    {
        this.Log(LogLevel.Information, message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        this.Log(LogLevel.Warning, message, args);
    }

    public void LogWarning(Exception exception, string message, params object[] args)
    {
        this.Log(LogLevel.Warning, $"{message} Exception: {exception}", args);
    }
}


public delegate void LogMessage(LogLevel logLevel, string message);

public class MauiLogger : BaseLogger
{
    public event LogMessage? OnLogMessage;

    protected override void Log(LogLevel logLevel, string message, object[] args)
    {
        string text;
        try{
            text = string.Format(ConvertMessageTemplate(message), args);
        }
        catch (Exception ex)
        {
            text = $"Error formatting log message: {ex.Message}. Original message: {message}";
        }

        OnLogMessage?.Invoke(logLevel, text);
    }

    private static string ConvertMessageTemplate(string message)
    {
        var argumentIndex = 0;

        return System.Text.RegularExpressions.Regex.Replace(
            message,
            "(?<!\\{)\\{([A-Za-z_][A-Za-z0-9_]*)(:[^}]*)?\\}(?!\\})",
            match => $"{{{argumentIndex++}{match.Groups[2].Value}}}");
    }
}
