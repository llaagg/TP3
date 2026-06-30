public interface INode
{
    string Id { get; }
    string Value { get; }
    public IEnumerable<INode> Leafs { get; }
}