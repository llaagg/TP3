using System.Net.Sockets;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Transport;

public class IpcSession : INetworkPipe
{
    public IpcSession(TcpClient client, NetworkStream stream, ITP3Transport transport, INetworkTransport networkTransport, string agentId)
    {
        Client = client;
        Stream = stream;
        TP3Transport = transport;
        Transport = networkTransport;
        AgentID = agentId;
    }

    public TcpClient Client { get; }

    public NetworkStream Stream { get; }

    public INetworkTransport Transport { get; }

    public ITP3Transport TP3Transport { get; }

    public string AgentID { get; }

    public Dictionary<string, INode> Pointers => throw new NotImplementedException();

}
