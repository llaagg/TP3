using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public class TP3Error : TP3Message
{
    public TP3Error(string error)
        : base(TP3Command.ERROR)
    {
        Error = error;
    }

    [ProtoMember(1)]
    public string Error { get; init; }
}
