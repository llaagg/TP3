using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("TP3.Agent.Service");

await TP3.CLI.CommandLineApplication.RunAsync(args, logger);

