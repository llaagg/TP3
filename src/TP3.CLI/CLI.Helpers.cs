using System.Reflection;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

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
    
    public static async Task<TP3Message?> Attach(this TP3Client ipcClient, ILogger logger)
    {
        var attachRequest = new TP3Message()
        {
            Tag = Guid.NewGuid().ToString("N").Substring(0, 8),
            AttachRequest = new TP3AttachRequest()
        };
        var attachResponse = await ipcClient.SendAndWaitOne(attachRequest, logger).ConfigureAwait(false);

        return attachResponse;
    }
    public static async Task<TP3Message?> Walk(this TP3Client ipcClient, string[] path, ILogger logger)
    {
        var walkRequest = new TP3Message()
        {
            Tag = Guid.NewGuid().ToString("N").Substring(0, 8),
            WalkRequest = new TP3WalkRequest()
            {
                Path = { path }
            }
        };
        var walkResponse = await ipcClient.SendAndWaitOne(walkRequest, logger).ConfigureAwait(false);

        return walkResponse;
    }


    public static void ThrowIfError(this TP3Message? message)
    {
        if (message is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if (message.Error is not null)
        {
            throw new InvalidOperationException($"Error received from IPC server: {message.Error?.Message}");
        }
    }
}
