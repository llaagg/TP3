using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

internal class AgentService : BaseDirectoryNode, IService
{
    private AgentHost agentHost;
    private ILogger? logger;
    private INetworkSessions networkSessions;

    public AgentService(AgentHost agentHost, ILogger? logger, INetworkSessions networkSessions)
        : base("AgentService")
    {
        this.agentHost = agentHost;
        this.logger = logger;
        this.networkSessions = networkSessions;

        State =  new StateNode(networkSessions);
    }

    public INode State;


    public async Task Init(IAgent me)
    {
        
    }

    public override IEnumerable<INode>? Children => new INode[] { State };
}