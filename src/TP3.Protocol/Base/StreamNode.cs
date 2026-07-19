using TP3.Messages;
using System.IO;
using TP3.Interfaces;
namespace TP3.Protocol;

public class StreamNode : INode
{
    public StreamNode(Stream stream, string? name = null, string? id = null)
    {
        // short guid if empty
        this.Id = id ?? Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        this.Name = name ?? this.GetType().Name;
        this.streamProvider = () => stream;
    }

    public StreamNode(Func<Stream> streamProvider, string? name = null, string? id = null)
    {
        // short guid if empty
        this.Id = id ?? Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        this.Name = name ?? this.GetType().Name;
        this.streamProvider = streamProvider;
    }

    public StreamNode(Func<string> streamProvider, string? name = null, string? id = null)
    {
        // short guid if empty
        this.Id = id ?? Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        this.Name = name ?? this.GetType().Name;
        this.streamProvider = () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(streamProvider()));
    }

    public string Id { get; protected set; } = null!;
    public string Name { get; protected set; } = null!;

    private Func<Stream> streamProvider;

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => new List<INode>();

    public ulong Length => 0;

    public virtual Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(new TP3Stream(streamProvider()));
    }
}