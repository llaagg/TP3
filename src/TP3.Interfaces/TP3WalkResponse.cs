namespace TP3.Messages;

public sealed class TP3WalkResponse : TP3Message
{
    public TP3WalkResponse()
        : base(TP3Command.WALK)
    {
    }

    public List<NodeInfo> Infos { get; set; } = new List<NodeInfo>();
}
