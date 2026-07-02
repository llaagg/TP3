using TP3.Interfaces;

namespace TP3.Messages;

public abstract class TP3Message
{
    protected TP3Message(TP3Command command, string? tag = null)
    {
        Command = command;
        
        Tag = tag ?? Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    public TP3Command Command { get; init; } = TP3Command.READ;

    /// <summary>
    /// The tag is used to match requests and responses. It is a string that is used 
    /// to identify user context. The server will echo the tag back in the response.
    /// </summary>
    public string Tag { get; init; } = string.Empty;
}
