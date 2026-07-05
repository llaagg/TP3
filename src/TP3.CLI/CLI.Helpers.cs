using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TP3.CLI;

public static partial class CLI
{
    public static ILogger InitilizeLogger(LogLevel logLevel)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                })
                .SetMinimumLevel(logLevel);
        });

        var logger = loggerFactory.CreateLogger(Assembly.GetExecutingAssembly().GetName().Name!);
        return logger;
    }
    
    private static async Task<TP3.Messages.TP3Message?> ReceiveSingleResponse(TP3Client ipcClient, ILogger logger)
    {
        logger.LogInformation("Listening for a single response from the IPC server...");
        await foreach (var response in ipcClient.ListenAsync())
        {
            return response;
        }

        return null;
    }
}
