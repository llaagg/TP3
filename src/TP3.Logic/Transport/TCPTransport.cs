using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Protocol;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public sealed partial class TCPTransport : INetworkTransport
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly TcpListener listener;
    private readonly Func<TP3Message, Task> responseFactory;
    private readonly SubscriptionManager subscriptionManager;
    private readonly Dictionary<string, StreamSession> streamSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object streamSessionsLock = new();
    private bool disposed;
    private readonly IRouter router;
    private readonly ILogger? logger;

    public TCPTransport(int port, IRouter router, ILogger? logger = null)
    {
        this.router = router;
        this.logger = logger;
        listener = new TcpListener(IPAddress.Any, port);
        subscriptionManager = new SubscriptionManager(logger);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cancellationTokenSource.Cancel();
        listener.Stop();
        cancellationTokenSource.Dispose();
    }

    public async Task Start()
    {
        try
        {
            listener.Start();
            logger?.LogInformation("Agent TCP listener started on port {Port}", listener.LocalEndpoint);
            Console.WriteLine($"Agent TCP listener started on port {listener.LocalEndpoint}");
            _ = Task.Run(() => AcceptLoopAsync(cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to start TCP listener.");
            Console.WriteLine($"Failed to start TCP listener: {ex.Message}");
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken));
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested.
            logger?.LogInformation("TCP accept loop canceled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "TCP accept loop error.");
            Console.WriteLine($"TCP accept loop error: {ex.Message}");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var networkStream = client.GetStream();
        var buffer = new byte[1024];

        try
        {
            this.logger?.LogInformation("Client connected: {ClientEndpoint}", client.Client.RemoteEndPoint);
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                {
                    break;
                }

                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                if (request.Equals("SUBSCRIBE", StringComparison.OrdinalIgnoreCase))
                {
                    await subscriptionManager.SubscribeClientAsync(client, networkStream, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (request.Equals("STREAM", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleStreamConnectionAsync(client, networkStream, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var message = TP3ProtocolHelpers.Parse(request);

                await router.Route(message);
                
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested.
            logger?.LogInformation("TCP client handler canceled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "TCP client handler error.");
            Console.WriteLine($"TCP client handler error: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private async Task HandleStreamConnectionAsync(TcpClient client, NetworkStream networkStream, CancellationToken cancellationToken)
    {
        await WriteLineAsync(networkStream, "STREAM_OK", cancellationToken).ConfigureAwait(false);

        try
        {
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var line = await ReadLineAsync(networkStream, cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                var parts = trimmed.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToUpperInvariant();

                switch (command)
                {
                    case "OPEN":
                        await HandleStreamOpenAsync(parts, networkStream, cancellationToken).ConfigureAwait(false);
                        break;
                    case "DATA":
                        await HandleStreamDataAsync(parts, networkStream, cancellationToken).ConfigureAwait(false);
                        break;
                    case "CLOSE":
                        await HandleStreamCloseAsync(parts, networkStream, cancellationToken).ConfigureAwait(false);
                        break;
                    case "QUIT":
                        return;
                    default:
                        await WriteLineAsync(networkStream, $"ERROR Unknown stream command: {command}", cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Stream client handler canceled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Stream client handler error.");
            Console.WriteLine($"Stream client handler error: {ex.Message}");
        }
        finally
        {
            client.Close();
            CleanupCompletedStreams();
        }
    }

    private async Task HandleStreamOpenAsync(string[] parts, NetworkStream networkStream, CancellationToken cancellationToken)
    {
        if (parts.Length < 5)
        {
            await WriteLineAsync(networkStream, "ERROR OPEN requires: OPEN <streamId> <path> <length> <contentType>", cancellationToken).ConfigureAwait(false);
            return;
        }

        var streamId = parts[1];
        var path = parts[2];
        if (!long.TryParse(parts[3], out var expectedLength) || expectedLength < 0)
        {
            await WriteLineAsync(networkStream, "ERROR Invalid length", cancellationToken).ConfigureAwait(false);
            return;
        }

        var contentType = parts[4];

        lock (streamSessionsLock)
        {
            if (streamSessions.ContainsKey(streamId))
            {
                WriteLineAsync(networkStream, $"ERROR Stream already exists: {streamId}", cancellationToken).GetAwaiter().GetResult();
                return;
            }

            var session = new StreamSession(streamId, path, expectedLength, contentType, logger);
            streamSessions.Add(streamId, session);
        }

        await WriteLineAsync(networkStream, $"OK OPEN {streamId}", cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleStreamDataAsync(string[] parts, NetworkStream networkStream, CancellationToken cancellationToken)
    {
        if (parts.Length < 5)
        {
            await WriteLineAsync(networkStream, "ERROR DATA requires: DATA <streamId> <seq> <final> <byteCount>", cancellationToken).ConfigureAwait(false);
            return;
        }

        var streamId = parts[1];
        if (!int.TryParse(parts[2], out var seq))
        {
            await WriteLineAsync(networkStream, "ERROR Invalid sequence number", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!bool.TryParse(parts[3], out var finalFlag))
        {
            await WriteLineAsync(networkStream, "ERROR Invalid final flag", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!int.TryParse(parts[4], out var byteCount) || byteCount < 0)
        {
            await WriteLineAsync(networkStream, "ERROR Invalid byte count", cancellationToken).ConfigureAwait(false);
            return;
        }

        var payload = await ReadExactAsync(networkStream, byteCount, cancellationToken).ConfigureAwait(false);

        StreamSession? session;
        lock (streamSessionsLock)
        {
            streamSessions.TryGetValue(streamId, out session);
        }

        if (session is null)
        {
            await WriteLineAsync(networkStream, $"ERROR Unknown stream: {streamId}", cancellationToken).ConfigureAwait(false);
            return;
        }

        await session.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await WriteLineAsync(networkStream, $"ACK {streamId} {seq}", cancellationToken).ConfigureAwait(false);

        if (finalFlag)
        {
            await session.CompleteAsync(cancellationToken).ConfigureAwait(false);
            lock (streamSessionsLock)
            {
                streamSessions.Remove(streamId);
            }
            await WriteLineAsync(networkStream, $"OK CLOSE {streamId}", cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleStreamCloseAsync(string[] parts, NetworkStream networkStream, CancellationToken cancellationToken)
    {
        if (parts.Length < 2)
        {
            await WriteLineAsync(networkStream, "ERROR CLOSE requires: CLOSE <streamId>", cancellationToken).ConfigureAwait(false);
            return;
        }

        var streamId = parts[1];
        StreamSession? session;
        lock (streamSessionsLock)
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
            await WriteLineAsync(networkStream, $"ERROR Unknown stream: {streamId}", cancellationToken).ConfigureAwait(false);
            return;
        }

        await session.CompleteAsync(cancellationToken).ConfigureAwait(false);
        await WriteLineAsync(networkStream, $"OK CLOSE {streamId}", cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new List<byte>();
        var single = new byte[1];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(single.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                return null;
            }

            if (single[0] == '\r')
            {
                continue;
            }

            if (single[0] == '\n')
            {
                break;
            }

            buffer.Add(single[0]);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static async Task WriteLineAsync(NetworkStream stream, string line, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(line + "\n");
        await stream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count, CancellationToken cancellationToken)
    {
        var buffer = new byte[count];
        var offset = 0;

        while (offset < count)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("Unexpected EOF while reading stream frame payload.");
            }

            offset += bytesRead;
        }

        return buffer;
    }

    private void CleanupCompletedStreams()
    {
        lock (streamSessionsLock)
        {
            foreach (var session in streamSessions.Values.ToArray())
            {
                session.Dispose();
            }

            streamSessions.Clear();
        }
    }

    public Task PublishEventAsync(string eventText)
    {
        return subscriptionManager.PublishEventAsync(eventText, cancellationTokenSource.Token);
    }

    public void Stop()
    {
        cancellationTokenSource.Cancel();
        listener.Stop();
        logger?.LogInformation("TCP listener stopped.");
    }
}
