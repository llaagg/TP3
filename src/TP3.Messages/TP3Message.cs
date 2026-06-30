namespace TP3.Messages;


public class TP3Message
{
    public TP3Command Command { get; init; } = TP3Command.HI;
    public string Target { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Target) && string.IsNullOrWhiteSpace(Payload);
}
