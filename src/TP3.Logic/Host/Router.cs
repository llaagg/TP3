using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public sealed class Router : IRouter
{
    private readonly IAgent agent;
    private readonly ITP3Logger? logger;

    public Router(IAgent agent, ITP3Logger? logger)
    {
        this.agent = agent;
        this.logger = logger;
    }

    public async Task Respond(IAgent agent, TP3Message message, INetworkPipe targetTransport)
    {
        logger?.LogDebug("Responding TP3 message: {Command}", message.PayloadCase);

        await targetTransport.TP3Transport.Send(targetTransport, message);
    }

    public async Task Route(INetworkPipe sourceTransport, TP3Message message)
    {
        logger?.LogDebug("Processing TP3 message: {Command}", message.PayloadCase);

#warning TODO: namespace filtering
#warning TODO: tcp forward, currelnty we only send to our local agent, but we should forward to other agents if the target is not local

        await agent.Handle(sourceTransport, message);
    }
}
