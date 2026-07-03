using System.Text.Json;

namespace TP3.Protocol;

public sealed class TP3StatPayloadStreamReader
{
    private readonly byte[] buffer;

    public TP3StatPayloadStreamReader(Stream data)
    {
        using var memory = new MemoryStream();
        data.CopyTo(memory);
        this.buffer = memory.ToArray();
    }

    public IEnumerable<TP3StatPayload> ReadAll()
    {
        var values = new List<TP3StatPayload>();

        foreach (var payload in EnumerateRootObjects(this.buffer))
        {
            var stat = JsonSerializer.Deserialize<TP3StatPayload>(payload, TP3StatPayloadExtensions.JsonOptions);
            if (stat != null)
            {
                values.Add(stat);
            }
        }

        return values;
    }

    private static IEnumerable<byte[]> EnumerateRootObjects(byte[] buffer)
    {
        var depth = 0;
        var inString = false;
        var escapeNext = false;
        var objectStart = -1;

        for (var index = 0; index < buffer.Length; index++)
        {
            var current = (char)buffer[index];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (current == '\\' && inString)
            {
                escapeNext = true;
                continue;
            }

            if (current == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (current == '{')
            {
                if (depth == 0)
                {
                    objectStart = index;
                }

                depth++;
                continue;
            }

            if (current == '}')
            {
                depth--;
                if (depth == 0 && objectStart >= 0)
                {
                    var length = index - objectStart + 1;
                    var slice = new byte[length];
                    Buffer.BlockCopy(buffer, objectStart, slice, 0, length);
                    yield return slice;
                    objectStart = -1;
                }
            }
        }
    }
}

