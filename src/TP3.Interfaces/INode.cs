using TP3.Messages;

namespace TP3.Interfaces
{
    public interface INode
    {
        string Qid { get; }

        string Name { get; }

        NodeType NodeType { get; }

        public IEnumerable<INode>? Children { get; }
    
    }
}