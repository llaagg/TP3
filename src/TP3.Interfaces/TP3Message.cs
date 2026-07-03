using TP3.Interfaces;
using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
[ProtoInclude(100, typeof(TP3WalkRequest))]
[ProtoInclude(101, typeof(TP3ReadRequest))]
[ProtoInclude(102, typeof(TP3AttachRequest))]
[ProtoInclude(103, typeof(TP3AttachResponse))]
[ProtoInclude(104, typeof(TP3ClunkRequest))]
[ProtoInclude(105, typeof(TP3ClunkResponse))]
[ProtoInclude(106, typeof(TP3Error))]
[ProtoInclude(107, typeof(TP3ErrorResponse))]
[ProtoInclude(108, typeof(TP3OpenRequest))]
[ProtoInclude(109, typeof(TP3OpenResponse))]
[ProtoInclude(110, typeof(TP3WalkResponse))]
[ProtoInclude(111, typeof(TP3ReadResponse))]
[ProtoInclude(112, typeof(TP3GenericMessage))]
public abstract class TP3Message
{
    protected TP3Message(TP3Command command, string? tag = null)
    {
        Command = command;
        
        Tag = tag ?? Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    [ProtoMember(1)]
    public TP3Command Command { get; init; } = TP3Command.READ;

    /// <summary>
    /// The tag is used to match requests and responses. It is a string that is used 
    /// to identify user context. The server will echo the tag back in the response.
    /// </summary>
    [ProtoMember(2)]
    public string Tag { get; init; } = string.Empty;
}
