using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

internal class AgentService : IService
{
    private AgentHost agentHost;
    private ILogger? logger;
    private INetworkSessions networkSessions;

    public AgentService(AgentHost agentHost, ILogger? logger, INetworkSessions networkSessions)
    {
        this.agentHost = agentHost;
        this.logger = logger;
        this.networkSessions = networkSessions;
    }

    public INode State => new StateNode(networkSessions);

    public INode Control => null!;

    public INode Events => null!;

    public async Task Init(IAgent me)
    {
    }
}