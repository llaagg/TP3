using System.Text;
using TP3.Messages;
using TP3.Service.FileSystem;

namespace TP3.Interfaces;

public static class ReaderChunkerEngine
{
    public const int DefaultMaxBytes = 16 * 1024;

    public static async Task<TP3ReadResponse> ReadAsync(
        TP3ReadRequest request,
        string qid,
        NodeType nodeType)
    {
        var offset = request.Offset < 0 ? 0 : request.Offset;
        var maxBytes = request.MaxBytes > 0 ? request.MaxBytes : DefaultMaxBytes;

        Reader reader = nodeType switch
        {
            NodeType.Directory => new DirectoryJsonReader(isRootState: false, qid),
            NodeType.File => new FileBinaryReader(qid),
            _ => throw new InvalidOperationException($"Unknown node type: {nodeType}.")
        };

        var result = await reader.ReadAsync(offset, maxBytes).ConfigureAwait(false);

        var data = result.IsEof
            ? Encoding.UTF8.GetBytes("EOF")
            : result.Data;

        return new TP3ReadResponse
        {
            Args = request.Args,
            Tag = request.Tag,
            Qid = qid,
            Offset = result.NextOffset,
            MaxBytes = maxBytes,
            NodeType = nodeType,
            IsChunk = true,
            ChunkIndex = offset > int.MaxValue ? int.MaxValue : (int)offset,
            IsFinalChunk = result.IsEof,
            Data = data
        };
    }
}