namespace TP3.Agent.Logic.Protocol;

public sealed class TP3Message
{
    public string Command { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public bool IsEmpty => string.IsNullOrWhiteSpace(Command);
}
