using System.Text.Json;

namespace TP3.Protocol;

public sealed class TP3StatPayloadReader
{
    private readonly byte[] buffer;

    public TP3StatPayloadReader(Stream data)
    {
        using var memory = new MemoryStream();
        data.CopyTo(memory);
        this.buffer = memory.ToArray();
    }

    public IEnumerable<TP3StatPayload> ReadAll()
    {
        var reader = new Utf8JsonReader(this.buffer, isFinalBlock: true, state: default);
        var values = new List<TP3StatPayload>();

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                continue;
            }

            var stat = JsonSerializer.Deserialize<TP3StatPayload>(ref reader, TP3StatPayloadExtensions.JsonOptions);
            if (stat != null)
            {
                values.Add(stat);
            }
        }

        return values;
    }
}

