using Microsoft.Extensions.Logging;

internal class TP3Logger : ITP3Logger
{
    private ILogger logger;

    public TP3Logger(ILogger logger)
    {
        this.logger = logger;
    }

    public void LogDebug(string message, params object[] args)
    {
        logger.LogDebug(message, args);
    }

    public void LogError(string message, params object[] args)
    {
        logger.LogError(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        logger.LogError(exception, message, args);
    }

    public void LogInformation(string message, params object[] args)
    {
        logger.LogInformation(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        logger.LogWarning(message, args);
    }

    public void LogWarning(Exception exception, string message, params object[] args)
    {
        logger.LogWarning(exception, message, args);
    }
}