using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Interfaces;

namespace TP3.GUI.GTK;

public static class AgentInstance
{
    public static AgentHost? Host { get; private set; }
    public static IAgent? Me => Host?.Me;
    public static ILogger? Logger { get; private set; }

    public static void Initialize(AgentHost host, ILogger logger)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
