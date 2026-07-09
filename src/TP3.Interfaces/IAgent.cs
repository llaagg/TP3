using TP3.Messages;

namespace TP3.Interfaces;

public interface IAgent
{    
    INode T { get; }
    Task AddService(IService service);
    Task Handle(INetworkPipe incomingTransport, TP3Message request);
}