using TP3.Messages;

namespace TP3.Interfaces;

public interface IAgent
{    
    INode T { get; }
    Task AddService(IService service);
    Task Handle(TP3Message request);
}