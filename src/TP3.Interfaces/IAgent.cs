using TP3.Messages;

namespace TP3.Interfaces;

public interface IAgent
{    
    INode T { get; }
    Task AddService(IService service);
    Task Handle(ITP3Transport incomingTransport, TP3Message request);
}