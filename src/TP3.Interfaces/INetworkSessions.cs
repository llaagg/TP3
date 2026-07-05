using TP3.Interfaces;

public interface INetworkSessions
{
    void AddSession(INetworkPipe session);
    void AttachTagToPointer(string tag, INode rootNode, INetworkPipe incomingNetworkSession);
    void CloseSession(INetworkPipe incomingTransport, string tag);
    INode FindNode(INetworkPipe incomingTransport, string tag);
    IPointer GetPointer(INetworkPipe incomingTransport, string tag);
}