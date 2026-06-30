public interface INode
{
    string Name { get; }
    public IEnumerable<INode>? Children { get; }
    public Stream? Data { get; }
}