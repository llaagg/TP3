using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public class RouteTP3Message : TP3Message
{
    public RouteTP3Message(TP3Message other) : base(other)
    {
        InnerMessage = other;
    }

    public TP3Message InnerMessage { get; }

    public INetworkTransport? IncomingTransport { get; set; }
}