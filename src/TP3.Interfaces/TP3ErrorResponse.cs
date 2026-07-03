namespace TP3.Messages;

public class TP3ErrorResponse : TP3Message
{
    public TP3ErrorResponse(string errorMessage)
        : base(TP3Command.ERROR)
    {
        this.ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; set; } = string.Empty;
}