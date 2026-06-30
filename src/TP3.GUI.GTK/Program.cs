using Gtk;
using Microsoft.Extensions.Logging;
using AgentType = TP3.Agent.Logic.Agent.Node;
using TP3.Agent.Logic.Host;
using TP3.GUI.GTK;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("TP3.GUI.GTK");
var host = AgentHost.Main();

AgentInstance.Initialize(host, logger);

logger.LogInformation("Starting TP3 GTK application.");

Application.Init();

var window = new TP3.GUI.GTK.MainWindow();
window.ShowAll();

Application.Run();
