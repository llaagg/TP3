namespace TP3.Messages;

public class TP3AttachResponse : TP3Message
{
    public TP3AttachResponse()
        : base(TP3Command.ATTACH)
    {
    }

    /// <summary>
    /// Return "/" alias trunk, sets client there
    /// </summary>
    public NodeInfo Info { get; set; } = new NodeInfo();
}
