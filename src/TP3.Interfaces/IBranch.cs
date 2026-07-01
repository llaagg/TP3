namespace TP3.Interfaces
{
    public interface INode
    {
        string Name { get; }

        public ITP3Stream? Data { get; }
    }
}