namespace TP3.Messages;

public class TP3AttachResponse : TP3Message
{
    public TP3AttachResponse()
        : base(TP3Command.ATTACH)
    {
    }

    public string Tag { get; set; } = string.Empty;
    public NodeType? NodeType { get; set; } = null;
}
