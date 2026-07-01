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
            await AcceptLoopAsync(cancellationTokenSource.Token);
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

        var session = new ClientSession(client, networkStream);
        var previousSession = currentSession.Value;
        currentSession.Value = session;

        try
        {
            logger?.LogInformation("IPC client connected: {ClientEndpoint}", client.Client.RemoteEndPoint);
            await ReadLoopAsync(session, cancellationToken).ConfigureAwait(false);
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
            TP3Message message;
            try
            {
                message = await TP3Serializer.ReadMessageAsync(session.Stream, cancellationToken).ConfigureAwait(false);
            }
            catch (EndOfStreamException)
            {
                // Client closed the connection cleanly.
                break;
            }
            catch (IOException ex)
            {
                logger?.LogWarning(ex, "IPC stream closed unexpectedly.");
                break;
            }
            catch (InvalidDataException ex)
            {
                logger?.LogWarning(ex, "Invalid TP3 packet received from IPC client.");
                break;
            }

            logger?.LogInformation("IPC RX: {Message}", message);

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

        var payload = TP3Serializer.SerializeBytes(message);
        await writeLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        try
        {
            await session.Stream.WriteAsync(payload.AsMemory(0, payload.Length), cancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private sealed class ClientSession
    {
        public ClientSession(TcpClient client, NetworkStream stream)
        {
            Client = client;
            Stream = stream;
        }

        public TcpClient Client { get; }
        public NetworkStream Stream { get; }
    }
}
