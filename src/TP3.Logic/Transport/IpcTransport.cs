using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Protocol;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public sealed class IpcTransport : INetworkTransport
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly AsyncLocal<ClientSession?> currentSession = new();
    private readonly TcpListener listener;
    private readonly IRouter router;
    private readonly ILogger? logger;
    private bool disposed;

    public IpcTransport(int port, IRouter router, ILogger? logger = null)
    {
        this.router = router;
        this.logger = logger;
        listener = new TcpListener(IPAddress.Loopback, port);
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
        writeLock.Dispose();
    }

    public async Task Start()
    {
        try
        {
            listener.Start();
            logger?.LogInformation("IPC listener started on port {Port}", listener.LocalEndpoint);
            Console.WriteLine($"IPC listener started on port {listener.LocalEndpoint}");
            _ = Task.Run(() => AcceptLoopAsync(cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to start IPC listener.");
            Console.WriteLine($"Failed to start IPC listener: {ex.Message}");
        }
    }

    public void Stop()
    {
        cancellationTokenSource.Cancel();
        listener.Stop();
        logger?.LogInformation("IPC listener stopped.");
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
            logger?.LogInformation("IPC accept loop canceled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "IPC accept loop error.");
            Console.WriteLine($"IPC accept loop error: {ex.Message}");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using var networkStream = client.GetStream();
        using var reader = new StreamReader(networkStream, Encoding.UTF8, leaveOpen: true);
        using var writer = new StreamWriter(networkStream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

        var session = new ClientSession(client, reader, writer);
        var previousSession = currentSession.Value;
        currentSession.Value = session;

        try
        {
            logger?.LogInformation("IPC client connected: {ClientEndpoint}", client.Client.RemoteEndPoint);

            var readTask = Task.Run(() => ReadLoopAsync(session, cancellationToken));
            await readTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("IPC client handler canceled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "IPC client handler error.");
            Console.WriteLine($"IPC client handler error: {ex.Message}");
        }
        finally
        {
            currentSession.Value = previousSession;
            client.Close();
        }
    }

    private async Task ReadLoopAsync(ClientSession session, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && session.Client.Connected)
        {
            var request = await session.Reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (request is null)
            {
                break;
            }

            var trimmed = request.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            Console.WriteLine($"IPC RX: {trimmed}");

            var message = TP3Serializer.Deserialize(trimmed);
            currentSession.Value = session;
            await router.Route(this, message).ConfigureAwait(false);
        }
    }

    public async Task Send(TP3Message message)
    {
        var session = currentSession.Value;
        if (session is null || !session.Client.Connected)
        {
            throw new InvalidOperationException("No active IPC client session is available for sending.");
        }

        var payload = TP3Serializer.SerializeText(message);
        await writeLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        try
        {
            await session.Writer.WriteLineAsync(payload).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public Task PublishEventAsync(string eventText)
    {
        Console.WriteLine($"IPC EVENT: {eventText}");
        return Task.CompletedTask;
    }

    private sealed class ClientSession
    {
        public ClientSession(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            Client = client;
            Reader = reader;
            Writer = writer;
        }

        public TcpClient Client { get; }
        public StreamReader Reader { get; }
        public StreamWriter Writer { get; }
    }
}
