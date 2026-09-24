using System.Reflection;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    var minlog = (Microsoft.Extensions.Logging.LogLevel)LogLevel.Information;
    builder
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
        })
        .SetMinimumLevel(minlog);
});

var logger = loggerFactory.CreateLogger(Assembly.GetExecutingAssembly().GetName().Name!);
var tp3Logger = new TP3Logger(logger);
await TP3.CLI.CommandLineApplication.RunAsync(args, tp3Logger);
