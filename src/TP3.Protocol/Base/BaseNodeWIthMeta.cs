using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Protocol.Base;

public abstract class BaseNodeWithMeta : INode
{
    public BaseNodeWithMeta()
    {
    }

    MetaData? _meta = new MetaData();

    public abstract string Id { get; }

    public abstract string Name { get; }

    public abstract NodeType NodeType { get; }

    public abstract IEnumerable<INode>? Children { get; }

    public abstract ulong Length { get; }

    public IMeta? GetMeta()
    {
        return _meta;
    }

    public abstract Task<ITP3DataStream?> Get();
}
