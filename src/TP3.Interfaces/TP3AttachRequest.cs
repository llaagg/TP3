namespace TP3.Messages;

public class TP3AttachRequest : TP3Message
{
    public TP3AttachRequest(string tag)
        : base(TP3Command.ATTACH)
    {
        Tag = tag;
    }
}
