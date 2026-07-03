using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Protocol;

public class TP3StatPayload
{
    public TP3StatPayload(INode node)
    {
        this.Name = node.Name;
        this.Info = new NodeInfo(node);
    }

    public string Name { get; private set; }

    public NodeInfo Info { get; private set; }

}


public static class TP3StatPayloadExtensions
{
    public static TP3StatPayload Deserilize(Stream data)
    {
        
        var stat = System.Text.Json.JsonSerializer.Deserialize<TP3StatPayload>(data, new System.Text.Json.JsonSerializerOptions
        {
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }
        });
        return stat!;
    }

    public static TP3StatPayload Deserilize(IEnumerable<byte> data)
    {
        var json = System.Text.Encoding.UTF8.GetString(data.ToArray());
        var stat = System.Text.Json.JsonSerializer.Deserialize<TP3StatPayload>(json, new System.Text.Json.JsonSerializerOptions
        {
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }
        });
        return stat!;
    }

    public static IEnumerable<byte> Serialize(this TP3StatPayload stat)
    {        
        // serilize as json and enums as asrings
        var json = System.Text.Json.JsonSerializer.Serialize(stat, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            }
        });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}

