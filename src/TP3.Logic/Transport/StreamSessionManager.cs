using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TP3.Agent.Logic.Transport;

public sealed class StreamSessionManager : IDisposable
{
    private readonly Dictionary<string, StreamSession> streamSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object lockObject = new();
    private readonly ILogger? logger;
    private bool disposed;

    public StreamSessionManager(ILogger? logger = null)
    {
        this.logger = logger;
    }

    public bool TryOpenSession(string streamId, string path, long expectedLength, string contentType, out string error)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            error = "Stream id cannot be empty.";
            return false;
        }

        lock (lockObject)
        {
            if (streamSessions.ContainsKey(streamId))
            {
                error = $"Stream already exists: {streamId}";
                return false;
            }

            var session = new StreamSession(streamId, path, expectedLength, contentType, logger);
            streamSessions.Add(streamId, session);
        }

        error = string.Empty;
        return true;
    }

    public async Task<(bool Success, bool Closed, string Error)> WriteFrameAsync(string streamId, byte[] payload, bool finalFlag, CancellationToken cancellationToken)
    {
        StreamSession? session;
        lock (lockObject)
        {
            streamSessions.TryGetValue(streamId, out session);
        }

        if (session is null)
        {
            return (false, false, $"Unknown stream: {streamId}");
        }

        try
        {
            await session.WriteAsync(payload, cancellationToken).ConfigureAwait(false);

            if (finalFlag)
            {
                await session.CompleteAsync(cancellationToken).ConfigureAwait(false);
                lock (lockObject)
                {
                    streamSessions.Remove(streamId);
                }

                return (true, true, string.Empty);
            }

            return (true, false, string.Empty);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to write stream frame for {StreamId}.", streamId);
            return (false, false, ex.Message);
        }
    }

    public bool TryCloseSession(string streamId, out string error)
    {
        StreamSession? session;
        lock (lockObject)
        {
            if (!streamSessions.TryGetValue(streamId, out session))
            {
                session = null;
            }
            else
            {
                streamSessions.Remove(streamId);
            }
        }

        if (session is null)
        {
            error = $"Unknown stream: {streamId}";
            return false;
        }

        try
        {
            session.CompleteAsync(CancellationToken.None).GetAwaiter().GetResult();
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to close stream {StreamId}.", streamId);
            error = ex.Message;
            return false;
        }
    }

    public void CleanupCompletedSessions()
    {
        lock (lockObject)
        {
            foreach (var session in streamSessions.Values)
            {
                session.Dispose();
            }

            streamSessions.Clear();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        CleanupCompletedSessions();
    }
}
