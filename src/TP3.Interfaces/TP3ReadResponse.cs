using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public sealed class TP3ReadResponse : TP3Message
{
    #warning get rid of TAG from this contract

    public TP3ReadResponse()
        : base(TP3Command.READ)
    {
    }
   
    [ProtoMember(1)]
    public byte[]? Data { get; set; }
}
