using TP3.Messages;

namespace TP3.Interfaces;

public class BaseDirectoryNode : INode
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

    public string Id { get; protected set; } = null!;

    public string Name { get; protected set; } = null!;

    private IList<INode> _children;

    public NodeType NodeType => NodeType.Directory;

    public virtual IEnumerable<INode>? Children => _children;

    public ulong Length => 0;

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }

    public void AddChild(INode state)
    {
        _children.Add(state);
    }

    public IMeta? GetMeta()
    {
        return null;
    }
}
