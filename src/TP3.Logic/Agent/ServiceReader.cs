namespace TP3.Interfaces;

public abstract class Reader
{
    public abstract Task<ServiceReadResult> ReadAsync(ulong offset, uint maxBytes);
}

public sealed class ServiceReadResult
{
    public required byte[] Data { get; init; }

    public required ulong NextOffset { get; init; }

    public required bool IsEof { get; init; }
}