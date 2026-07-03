namespace TP3.Messages;

public class TP3Error : TP3Message
{
    public TP3Error(string error)
        : base(TP3Command.ERROR)
    {
        Error = error;
    }

    public string Error { get; init; }
}
