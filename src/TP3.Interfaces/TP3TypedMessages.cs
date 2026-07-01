namespace TP3.Messages;

public sealed class TP3WalkRequest : TP3Message
{
    public TP3WalkRequest()
    {
        Command = TP3Command.WALK;
    }

    public static TP3WalkRequest From(TP3Message message)
    {
        if (message.Command != TP3Command.WALK)
        {
            throw new ArgumentException("Expected WALK command.", nameof(message));
        }

        return new TP3WalkRequest
        {
            Args = message.Args,
            Tag = message.Tag,
            Qid = message.Qid,
            Offset = message.Offset,
            MaxBytes = message.MaxBytes,
            NodeType = message.NodeType,
            IsChunk = message.IsChunk,
            ChunkIndex = message.ChunkIndex,
            IsFinalChunk = message.IsFinalChunk,
            Data = message.Data,
            Error = message.Error
        };
    }
}

public sealed class TP3ReadRequest : TP3Message
{
    public TP3ReadRequest()
    {
        Command = TP3Command.READ;
    }

    public static TP3ReadRequest From(TP3Message message)
    {
        if (message.Command != TP3Command.READ)
        {
            throw new ArgumentException("Expected READ command.", nameof(message));
        }

        return new TP3ReadRequest
        {
            Args = message.Args,
            Tag = message.Tag,
            Qid = message.Qid,
            Offset = message.Offset,
            MaxBytes = message.MaxBytes,
            NodeType = message.NodeType,
            IsChunk = message.IsChunk,
            ChunkIndex = message.ChunkIndex,
            IsFinalChunk = message.IsFinalChunk,
            Data = message.Data,
            Error = message.Error
        };
    }
}

public sealed class TP3WalkResponse : TP3Message
{
    public TP3WalkResponse()
    {
        Command = TP3Command.WALK;
    }
}

public sealed class TP3ReadResponse : TP3Message
{
    public TP3ReadResponse()
    {
        Command = TP3Command.READ;
    }
}