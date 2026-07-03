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
        var objectStart = -1;

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 0 && objectStart >= 0)
                {
                    var length = (int)reader.BytesConsumed - objectStart;
                    var payload = this.buffer.AsSpan(objectStart, length);
                    var stat = JsonSerializer.Deserialize<TP3StatPayload>(payload, TP3StatPayloadExtensions.JsonOptions);
                    if (stat != null)
                    {
                        values.Add(stat);
                    }

                    objectStart = -1;
                }

                continue;
            }

            if (reader.CurrentDepth == 0)
            {
                objectStart = (int)reader.TokenStartIndex;
            }
        }

        return values;
    }
}

