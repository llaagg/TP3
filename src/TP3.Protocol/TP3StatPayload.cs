using System.Text.Json;
using System.Text.Json.Serialization;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Protocol;

[JsonSerializable(typeof(TP3StatPayload))]
public class TP3StatPayload
{
    public TP3StatPayload()
    {
    }

    public TP3StatPayload(INode node)
    {
        this.Name = node.Name;
        this.Info = new NodeInfo
        {
            Id = node.Id,
            NodeType = node.NodeType,
        };
    }

    public string Name { get; set; } = string.Empty;

    public NodeInfo Info { get; set; } = new NodeInfo();
}
