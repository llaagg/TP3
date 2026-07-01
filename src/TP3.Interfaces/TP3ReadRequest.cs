namespace TP3.Messages;

public sealed class TP3ReadRequest : TP3Message
{
    private List<string> _args = new();

    public TP3ReadRequest()
    {
        Command = TP3Command.READ;
    }

    public new List<string> Args
    {
        get => _args;
        init => _args = value ?? new List<string>();
    }

    public new string? Tag { get; init; }

    public string? Qid { get; init; }

    public long Offset { get; init; }

    public int MaxBytes { get; init; }

    public static TP3ReadRequest From(TP3Message message)
    {
        if (message.Command != TP3Command.READ)
        {
            throw new ArgumentException("Expected READ command.", nameof(message));
        }

        var qid = message is TP3ReadRequest readRequest ? readRequest.Qid
            : message is TP3ReadResponse readResponse ? readResponse.Qid
            : null;
        var offset = message is TP3ReadRequest readRequestOffset ? readRequestOffset.Offset
            : message is TP3ReadResponse readResponseOffset ? readResponseOffset.Offset
            : 0;
        var maxBytes = message is TP3ReadRequest readRequestMaxBytes ? readRequestMaxBytes.MaxBytes
            : message is TP3ReadResponse readResponseMaxBytes ? readResponseMaxBytes.MaxBytes
            : 0;

        return new TP3ReadRequest
        {
            Args = message.Args,
            Tag = message.Tag,
            Qid = qid,
            Offset = offset,
            MaxBytes = maxBytes
        };
    }
}
