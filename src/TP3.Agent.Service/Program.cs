using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;

Console.WriteLine("Starting agent service...");

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});



var agent = AgentHost.Main(args, logger: loggerFactory.CreateLogger("TP3.Agent.Service"));

