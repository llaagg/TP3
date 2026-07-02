namespace TP3.Messages;

public sealed class TP3WalkResponse : TP3Message
{
    public TP3WalkResponse()
        : base(TP3Command.WALK)
    {
    }

    public string? Qid { get; init; }

    public NodeType? NodeType { get; init; }

    public string? Error { get; init; }
}
