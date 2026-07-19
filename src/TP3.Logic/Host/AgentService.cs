using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Agent.Logic.Host;

internal class AgentService : BaseDirectoryNode, IService
{
    private NetworkManager agentHost;
    private ILogger? logger;
    private INetworkSessions networkSessions;

    public AgentService(NetworkManager agentHost, ILogger? logger, INetworkSessions networkSessions)
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

    public Task Start()
    {
        throw new NotImplementedException();
    }

    public Task Stop()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }

    public override IEnumerable<INode>? Children => new INode[] { State };
}