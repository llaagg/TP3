using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TP3.Agent.Logic.Host;

public sealed class StreamSession : IDisposable
{
    public string StreamId { get; }
    public string Path { get; }
    public long ExpectedLength { get; }
    public string ContentType { get; }
    public string TempFilePath { get; }

    private readonly FileStream fileStream;
    private readonly ILogger? logger;

    public StreamSession(string streamId, string path, long expectedLength, string contentType, ILogger? logger)
    {
        StreamId = streamId;
        Path = path;
        ExpectedLength = expectedLength;
        ContentType = contentType;
        this.logger = logger;

        TempFilePath = System.IO.Path.GetTempFileName();
        fileStream = new FileStream(TempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan);
        logger?.LogInformation("Created stream session {StreamId} writing to {TempFilePath}", streamId, TempFilePath);
    }

    public async Task WriteAsync(byte[] data, CancellationToken cancellationToken)
    {
        await fileStream.WriteAsync(data.AsMemory(0, data.Length), cancellationToken).ConfigureAwait(false);
        logger?.LogDebug("Wrote {ByteCount} bytes to stream {StreamId}", data.Length, StreamId);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        fileStream.Close();
        logger?.LogInformation("Stream {StreamId} closed with {Length} bytes at {TempFilePath}", StreamId, fileStream.Length, TempFilePath);
    }

    public void Dispose()
    {
        try
        {
            fileStream.Dispose();
        }
        catch
        {
        }

        try
        {
            if (System.IO.File.Exists(TempFilePath))
            {
                System.IO.File.Delete(TempFilePath);
            }
        }
        catch
        {
        }
    }
}
