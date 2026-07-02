namespace TP3.Interfaces;

/// <summary>
/// Present physical pipe to a remote agent. This is the physical connection that can be used to send and receive messages.
/// </summary>
public interface INetworkPipe
{
    public INetworkTransport Transport { get; }
    public ITP3Transport TP3Transport { get; }
    public string AgentID { get; }
}
