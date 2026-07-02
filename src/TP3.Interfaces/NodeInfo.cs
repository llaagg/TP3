using TP3.Interfaces;

namespace TP3.Messages;

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
    
    public string? Id { get; set; } = null;
    public NodeType? NodeType { get; set; } = null;
}