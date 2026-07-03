using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public class TP3ErrorResponse : TP3Message
{
    public TP3ErrorResponse(string errorMessage)
        : base(TP3Command.ERROR)
    {
        this.ErrorMessage = errorMessage;
    }

    [ProtoMember(1)]
    public string ErrorMessage { get; set; } = string.Empty;
}