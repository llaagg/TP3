using Gtk;
using Microsoft.Extensions.Logging;



var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

AgentInstance.Logger = loggerFactory.CreateLogger("TP3.GUI.GTK");
AgentInstance.Logger.LogInformation("Starting TP3 GTK application.");

// start agent
AgentInstance.Agent = new TP3.Agent.Logic.Agent.Node(AgentInstance.Logger as ILogger<TP3.Agent.Logic.Agent.Node>);
AgentInstance.Connection.Connect();
AgentInstance.Logger.LogInformation("Agent and connection initialized.");

Application.Init();

var window = new TP3.GUI.GTK.MainWindow();
window.ShowAll();

Application.Run();
