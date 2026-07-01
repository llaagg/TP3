namespace TP3.Interfaces;

public abstract class Reader
{
    public abstract Task<ServiceReadResult> ReadAsync(long offset, int maxBytes);
}

public sealed class ServiceReadResult
{
    public required byte[] Data { get; init; }

    public required long NextOffset { get; init; }

    public required bool IsEof { get; init; }
}