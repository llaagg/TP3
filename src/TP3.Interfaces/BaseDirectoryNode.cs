using TP3.Messages;

namespace TP3.Interfaces;

public class BaseDirectoryNode : INode
{
    public BaseDirectoryNode(string? name = null, string? id = null)
    {
        // short guid if empty
        this.Id = id ?? Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        // put - before all capital letter make to lowercase and trim - from beginning and end
        this.Name = name ?? NameCreator(this.GetType());
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

    public NodeType NodeType => NodeType.Directory;

    public virtual IEnumerable<INode>? Children => new List<INode>();
    
    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
}
