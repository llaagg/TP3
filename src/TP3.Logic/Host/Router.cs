using System;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Protocol;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public sealed class Router : IRouter
{
    private readonly AgentHost host;
    private readonly ILogger? logger;

    public Router(AgentHost host, ILogger? logger)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.logger = logger;
    }

    public async Task Respond(IAgent agent, TP3Message request, TP3Message response)
    {
        // someone asked about this while ago, and i can respond to it but where :-)
        if(request is RouteTP3Message routeMessage && routeMessage.IncomingTransport != null)
        {
            logger?.LogDebug("Responding to request {RequestCommand} with response {ResponseCommand} via transport {TransportType}.", request.Command, response.Command, routeMessage.IncomingTransport.GetType().Name);
            await routeMessage.IncomingTransport.Send(response);
        }
        else
        {
            logger?.LogWarning("Request is null, cannot respond.");
            return;
        }
    }

    public async Task Route(INetworkTransport ipcTransport, TP3Message message)
    {
        logger?.LogDebug("Routing TP3 message: {Command} {Path}", message.Command, message.Args);

        #warning TODO: namespace filtering
        #warning TODO: tcp forward, currelnty we only send to our local agent, but we should forward to other agents if the target is not local
        
        await host.Me.Handle(new RouteTP3Message(message) { IncomingTransport = ipcTransport });
    }
}
