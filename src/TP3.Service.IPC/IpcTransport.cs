using System.Net;
using System.Net.Sockets;
using TP3.Agent.Logic.Protocol;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public class IpcTransport : INetworkTransport
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly AsyncLocal<IpcSession?> currentSession = new();
    private readonly TcpListener listener;
    private ITP3Transport transport = null!;
    private readonly ILogger? logger;
    private bool disposed;
    private bool Initialized;

    public IpcTransport(int port, ILogger? logger = null)
    {
        this.logger = logger;
        listener = new TcpListener(IPAddress.Loopback, port);
    }

    public string Describe()
    {
        return $"IPC Transport on {listener.LocalEndpoint}";
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
        if(this.Initialized == false)
        {
            throw new InvalidOperationException("Transport is not initialized. Call Init() before Start().");
        }

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
    
        var session = new IpcSession(client, networkStream, this.transport, this, nameof(IpcTransport));

        this.transport.NewUserNetworkConnection(session);

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

    private async Task ReadLoopAsync(IpcSession session, CancellationToken cancellationToken)
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
    
            await transport.Route(session, message).ConfigureAwait(false);

            currentSession.Value = null;
        }
    }

    public async Task Send(INetworkPipe session, TP3Message message)
    {
        var s = session as IpcSession;
        if (s == null)
        {
            throw new InvalidOperationException("Invalid session type for IPC transport.");
        }

        var payload = TP3Serializer.SerializeBytes(message);
        await writeLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        try
        {
            await s.Stream.WriteAsync(payload.AsMemory(0, payload.Length), cancellationTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public Task Init(ITP3Transport tp3CommunicationHandler)
    {
        this.transport = tp3CommunicationHandler;

        this.Initialized = true;
        return Task.CompletedTask;
    }
}
