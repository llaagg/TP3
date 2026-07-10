using System.Net.Sockets;
using TP3.Interfaces;

namespace TP3.Service.Attached;

public class TcpServerSession : INetworkPipe
{
    public TcpServerSession(TcpClient client, NetworkStream stream, ITP3Transport transport, INetworkTransport networkTransport, string agentId)
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

    public override string ToString()
    {
        return Client.Client.RemoteEndPoint?.ToString() ?? AgentID;
    }
}