namespace TP3.Messages;

public sealed class TP3ReadResponse : TP3Message
{
    public TP3ReadResponse()
    {
        Command = TP3Command.READ;
    }

    public string? Qid { get; init; }

    public long Offset { get; init; }

    public int MaxBytes { get; init; }

    public NodeType? NodeType { get; init; }

    public bool IsChunk { get; init; }

    public int ChunkIndex { get; init; }

    public bool IsFinalChunk { get; init; }

    public byte[]? Data { get; init; }

    public string? Error { get; init; }
}