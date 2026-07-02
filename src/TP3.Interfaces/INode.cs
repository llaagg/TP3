using TP3.Messages;

namespace TP3.Interfaces
{
    public interface INode
    {
        string Qid { get; }

        string Name { get; }

        NodeType NodeType { get; }

        IEnumerable<INode>? Children { get; }

        /// <summary>
        /// Needs to be implemented only for nodes that are not of type directory.
        /// If <see cref="NodeType"/> is <see cref="NodeType.Directory"/>, this method should return null.
        /// If it will not be null, then custom implementation that is compatible with direcotry handling should be provided.
        /// </summary>
        /// <returns></returns>
        Task<ITP3DataStream?> Get();
    }
}