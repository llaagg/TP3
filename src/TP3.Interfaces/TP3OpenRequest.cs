namespace TP3.Messages;

public class TP3OpenRequest : TP3Message
{
    public TP3OpenRequest(string tag)
        : base(TP3Command.OPEN)
    {
        Tag = tag;
    }
}
