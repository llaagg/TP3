public interface INode
{
    string Name { get; }

    /// <summary>
    /// This is null if node has data. This is empty array (count = 0) if node has no children ex.: empty folder
    /// </summary>
    public IEnumerable<INode>? Children { get; }

    /// <summary>
    /// This is null if node has children. This is empty stream if node has no data ex.: empty file
    /// </summary>
    public Stream? Data { get; }
}