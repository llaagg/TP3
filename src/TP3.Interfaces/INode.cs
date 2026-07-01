namespace TP3.Interfaces
{
    public interface INode
    {
        string Name { get; }

        public IEnumerable<INode>? Children{ get; }
    
        public ITP3Stream? Data { get; }
    }
}