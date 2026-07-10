using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Protocol;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.Attached;

public class TcpServerTransport : INetworkTransport
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly TcpListener listener;
    private readonly ILogger? logger;
    private ITP3Transport transport = null!;
    private bool disposed;
    private bool initialized;

    public TcpServerTransport(int port, ILogger? logger = null)
    {
        this.logger = logger;
        listener = new TcpListener(IPAddress.Any, port);
    }

    public string Describe()
    {
        return $"TCP Server Transport on {listener.LocalEndpoint}";
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
        if (!initialized)
        {
            throw new InvalidOperationException("Transport is not initialized. Call Init() before Start().");
        }

        try
        {
            listener.Start();
            logger?.LogInformation("TCP server listener started on {Endpoint}", listener.LocalEndpoint);
            await AcceptLoopAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to start TCP server listener.");
            Console.WriteLine($"Failed to start TCP server listener: {ex.Message}");
        }
    }

    public void Stop()
    {
        cancellationTokenSource.Cancel();
        listener.Stop();
        logger?.LogInformation("TCP server listener stopped.");
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
            logger?.LogInformation("TCP server accept loop canceled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "TCP server accept loop error.");
            Console.WriteLine($"TCP server accept loop error: {ex.Message}");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using var networkStream = client.GetStream();

        var session = new TcpServerSession(client, networkStream, this.transport, this, Guid.NewGuid().ToString("N")[..8]);

        this.transport.NewUserNetworkConnection(session);

        try
        {
            logger?.LogInformation("TCP server client connected: {ClientEndpoint}", client.Client.RemoteEndPoint);
            await ReadLoopAsync(session, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("TCP server client handler canceled.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "TCP server client handler error.");
            Console.WriteLine($"TCP server client handler error: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    private async Task ReadLoopAsync(TcpServerSession session, CancellationToken cancellationToken)
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
                break;
            }
            catch (IOException ex)
            {
                logger?.LogWarning(ex, "TCP server stream closed unexpectedly.");
                break;
            }
            catch (InvalidDataException ex)
            {
                logger?.LogWarning(ex, "Invalid TP3 packet received from TCP client.");
                break;
            }

            logger?.LogInformation("TCP server RX: {Message}", message);

            await transport.Route(session, message).ConfigureAwait(false);
        }
    }

    public async Task Send(INetworkPipe session, TP3Message message)
    {
        if (session is not TcpServerSession tcpSession)
        {
            throw new InvalidOperationException("Invalid session type for TCP server transport.");
        }

        var payload = TP3Serializer.SerializeBytes(message);
        await writeLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        try
        {
            await tcpSession.Stream.WriteAsync(payload.AsMemory(0, payload.Length), cancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public Task Init(ITP3Transport tp3CommunicationHandler)
    {
        this.transport = tp3CommunicationHandler;
        this.initialized = true;
        return Task.CompletedTask;
    }
}