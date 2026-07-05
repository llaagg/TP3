using System.Reflection;
using Microsoft.Extensions.Logging;
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
    
}
