using TP3.Messages;

namespace TP3.Interfaces;

public interface IAgent : IDisposable
{    
    INode T { get; }
    Task Handle(INetworkPipe incomingTransport, TP3Message request);
    Task Init();
    Task Start();
    Task Stop();
}