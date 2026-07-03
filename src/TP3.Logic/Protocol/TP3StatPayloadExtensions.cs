using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TP3.Protocol;

public static class TP3StatPayloadExtensions
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static IEnumerable<TP3StatPayload> Deserilize(Stream data)
    {
        return new TP3StatPayloadReader(data).ReadAll();
    }

    public static TP3StatPayload Deserilize(IEnumerable<byte> data)
    {
        var json = Encoding.UTF8.GetString(data.ToArray());
        var stat = JsonSerializer.Deserialize<TP3StatPayload>(json, JsonOptions);
        return stat!;
    }

    public static IEnumerable<byte> Serialize(this TP3StatPayload stat)
    {
        // serilize as json and enums as asrings
        var json = JsonSerializer.Serialize(stat, new JsonSerializerOptions(JsonOptions)
        {
            WriteIndented = true
        });
        return Encoding.UTF8.GetBytes(json);
    }


}

