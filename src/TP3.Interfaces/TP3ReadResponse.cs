namespace TP3.Messages;

public sealed class TP3ReadResponse : TP3Message
{
    #warning get rid of TAG from this contract

    public TP3ReadResponse()
        : base(TP3Command.READ)
    {
    }
   
    public byte[]? Data { get; set; }
}
