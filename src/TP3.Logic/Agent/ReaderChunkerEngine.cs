using System.Text;
using TP3.Messages;
using TP3.Service.FileSystem;

namespace TP3.Interfaces;

public static class ReaderChunkerEngine
{
    public const int DefaultMaxBytes = 16 * 1024;

    public static async Task<TP3ReadResponse> ReadAsync(
        TP3ReadRequest request,
        INode node)
    {
        var offset = request.Offset < 0 ? 0 : request.Offset;
        var maxBytes = request.MaxBytes > 0 ? request.MaxBytes : DefaultMaxBytes;

        Reader reader = node.NodeType switch
        {
            NodeType.Directory => new DirectoryJsonReader(node),
            NodeType.File => new FileBinaryReader(node.Qid),
            _ => throw new InvalidOperationException($"Unknown node type: {node.NodeType}.")
        };

        var result = await reader.ReadAsync(offset, maxBytes).ConfigureAwait(false);

        var data = result.IsEof
            ? Encoding.UTF8.GetBytes("EOF")
            : result.Data;

        return new TP3ReadResponse
        {
            Tag = request.Tag,
            Qid = node.Qid,
            Offset = result.NextOffset,
            MaxBytes = maxBytes,
            NodeType = node.NodeType,
            IsChunk = true,
            ChunkIndex = offset > int.MaxValue ? int.MaxValue : (int)offset,
            IsFinalChunk = result.IsEof,
            Data = data
        };
    }
}