using Gtk;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.GUI.GTK;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
        })
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("TP3.GUI.GTK");
var host = new AgentHost(5000, 5001, logger, new IService[]
{
    
});

AgentInstance.Initialize(host, logger);

logger.LogInformation("Starting TP3 GTK application.");

Application.Init();

var window = new TP3.GUI.GTK.MainWindow();
window.ShowAll();

Application.Run();
