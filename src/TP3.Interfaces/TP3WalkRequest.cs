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

public class TP3Error : TP3Message
{
    public TP3Error(string error)
        : base(TP3Command.ERROR)
    {
        Error = error;
    }

    public string Error { get; init; }
}
