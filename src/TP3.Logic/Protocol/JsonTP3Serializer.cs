using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

internal sealed class JsonTP3Serializer : ITP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public byte[] Header { get; } = Utf8.GetBytes("TP31");
    public TP3SerializationFormat Format => TP3SerializationFormat.Json;

    public byte[] SerializePayload(TP3Message message)
        => Utf8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

    public TP3Message DeserializePayload(ReadOnlySpan<byte> payload)
    {
        var json = Utf8.GetString(payload);
        return JsonSerializer.Deserialize<TP3Message>(json, JsonOptions)
            ?? new TP3Message();
    }
}
