using ProtoBuf;
using TP3.Interfaces;

namespace TP3.Messages;

[ProtoContract]
public class NodeInfo
{
    public NodeInfo()
    {
    }

    public NodeInfo(string? id, NodeType? nodeType)
    {
        Id = id;
        NodeType = nodeType;
    }

    public NodeInfo(INode node)
        : this(node.Id, node.NodeType)
    {
    }
    
    [ProtoMember(1)]
    public string? Id { get; set; } = null;

    [ProtoMember(2)]
    public NodeType? NodeType { get; set; } = null;
}