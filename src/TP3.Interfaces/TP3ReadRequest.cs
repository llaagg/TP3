namespace TP3.Messages;

public sealed class TP3ReadRequest : TP3Message
{
    public TP3ReadRequest()
        : base(TP3Command.READ)
    {
    }

    public string? Qid { get; init; }

    public long Offset { get; init; }

    public int MaxBytes { get; init; }
}
