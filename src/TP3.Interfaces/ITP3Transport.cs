using System;
using System.Threading.Tasks;
using TP3.Messages;

namespace TP3.Interfaces;

public interface ITP3Transport : IDisposable
{
    /// <summary>
    /// Uniq tag for all transports
    /// </summary>
    public string Tag { get; } 
    Task Send(INetworkPipe session, TP3Message message);
    Task Start();
    void Stop();
    Task Init(IRouter router);
    void NewUserNetworkConnection(INetworkTransport ipcTransport, INetworkPipe session);
    INode GetNode(INetworkPipe incomingTransport, string tag);
    void AttachTag(string tag, INode rootNode, INetworkPipe incomingNetworkSession);
    Task<ITP3DataStream> GetData(INetworkPipe incomingTransport, string tag);
    IPointer GetPointer(INetworkPipe incomingTransport, string tag);
    Task Route(INetworkPipe session, TP3Message message);
}
