using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public class RouteTP3Message
{
    public RouteTP3Message(TP3Message other) 
    {
        InnerMessage = other;
    }

    public TP3Message InnerMessage { get; }

    public ITP3Transport? IncomingTransport { get; set; }
}