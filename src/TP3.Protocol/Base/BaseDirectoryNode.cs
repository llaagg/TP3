using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Protocol.Base;

public class BaseDirectoryNode : BaseNodeWithMeta
{
    public BaseDirectoryNode()
        : this(null, null, null)
    {
    }

    public BaseDirectoryNode(IList<INode>? children = null)
        : this(null, null, children)
    {
    }

    public BaseDirectoryNode(string? name = null, string? id = null, IList<INode>? children = null)
    {
        // short guid if empty
        this.Id = id ?? Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        // put - before all capital letter make to lowercase and trim - from beginning and end
        this.Name = name ?? NameCreator(this.GetType());
        this._children = children ?? new List<INode>();
    }

    public static string NameCreator(Type type)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            type.Name,
            "([A-Z])",
            "-$1"
        ).ToLower().Trim('-');
    }

    public override string Id { get; } = null!;

    public override string Name { get; } = null!;

    private IList<INode> _children;

    public override NodeType NodeType => NodeType.Directory;

    public override IEnumerable<INode>? Children => _children;

    public override ulong Length => 0;


    public override Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }

    public void AddChild(INode state)
    {
        _children.Add(state);
    }
}
