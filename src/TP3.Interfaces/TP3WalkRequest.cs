namespace TP3.Messages;

public sealed class TP3WalkRequest : TP3Message
{
    public TP3WalkRequest(
        string tag,
        string newTag = null!,
        List<string>? path = null)
        : base(TP3Command.WALK)
    {
        Path = path ?? new List<string>();
        Tag = tag;
        NewTag = newTag;
    }

    public List<string> Path { get; set; } = new List<string>();
    public string Tag { get; set; } = string.Empty;
    public string? NewTag { get; set; } = null!;
}
