namespace TP3.Messages;

public sealed class TP3ReadRequest : TP3Message
{
    public TP3ReadRequest(string? tag = null)
        : base(TP3Command.READ)
    {
        if (tag != null)
        {
            Tag = tag;
        }
    }

    public long Offset { get; init; }

    public int MaxBytes { get; init; }
}
