namespace TP3.Messages;


public class TP3Message
{
    public TP3Command Command { get; init; } = TP3Command.NONE;
    public string Target { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public bool IsEmpty => Command == TP3Command.NONE && string.IsNullOrWhiteSpace(Target) && string.IsNullOrWhiteSpace(Payload);
}
