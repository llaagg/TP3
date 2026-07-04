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
        return new TP3StatPayloadStreamReader(data).ReadAll();
    }

    public static IEnumerable<byte> Serialize(this TP3StatPayload stat)
    {
        // serilize as json and enums as asrings
        var json = SerializeToJson(stat);
        return Encoding.UTF8.GetBytes(json);
    }


    public static string SerializeToJson(this TP3StatPayload stat)
    {
        // Zamiast: JsonSerializer.Serialize(stat)
        return JsonSerializer.Serialize(stat, TP3JsonContext.Default.TP3StatPayload);
    }
    
    public static TP3StatPayload DeserializeFromJson(string json)
    {
        return JsonSerializer.Deserialize<TP3StatPayload>(json, TP3JsonContext.Default.TP3StatPayload) ?? new TP3StatPayload();
    }

    
    public static TP3StatPayload DeserializeFromJson(byte[] json)
    {
        return JsonSerializer.Deserialize<TP3StatPayload>(json, TP3JsonContext.Default.TP3StatPayload) ?? new TP3StatPayload();
    }
}


[JsonSerializable(typeof(TP3StatPayload))]
internal partial class TP3JsonContext : JsonSerializerContext
{
}

