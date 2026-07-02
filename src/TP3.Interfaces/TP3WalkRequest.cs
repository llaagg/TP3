namespace TP3.Messages;

public sealed class TP3WalkRequest : TP3Message
{
    public TP3WalkRequest(List<string>? path = null)
        : base(TP3Command.WALK)
    {
        Path = path ?? new List<string>();
    }

    public List<string> Path { get; init; } = new List<string>();
}
