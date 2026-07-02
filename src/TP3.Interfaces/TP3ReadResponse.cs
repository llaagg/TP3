using TP3.Interfaces;

namespace TP3.Messages;

public sealed class TP3ReadResponse : TP3Message
{
    #warning get rid of TAG from this contract

    public TP3ReadResponse()
        : base(TP3Command.READ)
    {
    }
    public byte[]? Data { get; set; }
}

public class TP3Stat
{
    public TP3Stat(INode node)
    {
        this.Name = node.Name;
        this.Info = new NodeInfo(node);
    }

    public string Name { get; private set; }

    public NodeInfo Info { get; private set; }

    public static IEnumerable<TP3Stat> DataAsFolders(IEnumerable<TP3ReadResponse> data)
    {
        using var dataStream = new ReadResponseDataStream(data);

        // Stream JSON objects from merged chunks to avoid materializing all results.
        var stats = System.Text.Json.JsonSerializer.DeserializeAsyncEnumerable<TP3Stat>(dataStream);
        var enumerator = stats.GetAsyncEnumerator();

        try
        {
            while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
            {
                if (enumerator.Current is not null)
                {
                    yield return enumerator.Current;
                }
            }
        }
        finally
        {
            enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public IEnumerable<byte> Serialize()
    {
        // serilize as json and enums as asrings
        var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
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
