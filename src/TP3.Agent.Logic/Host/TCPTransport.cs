using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TP3.Agent.Logic.Host;

public sealed class TCPTransport : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly TcpListener listener;
    private readonly Func<string, string> responseFactory;
    private readonly List<Subscriber> subscribers = new();
    private readonly object subscribersLock = new();
    private bool disposed;
    private readonly ILogger? logger;

    public TCPTransport(int port, Func<string, string> responseFactory, ILogger? logger = null)
    {
        this.responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        this.logger = logger;
        listener = new TcpListener(IPAddress.Any, port);
        Start(port);
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

    private void Start(int port)
    {
        try
        {
            listener.Start();
            logger?.LogInformation("Agent TCP listener started on port {Port}", port);
            Console.WriteLine($"Agent TCP listener started on port {port}");
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
                if (request.Equals("SUBSCRIBE", StringComparison.OrdinalIgnoreCase) || request.Equals("STREAM", StringComparison.OrdinalIgnoreCase))
                {
                    await SubscribeClientAsync(client, networkStream, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var responseText = responseFactory(request);
                var responseBytes = Encoding.UTF8.GetBytes(responseText + "\n");
                await networkStream.WriteAsync(responseBytes.AsMemory(0, responseBytes.Length), cancellationToken).ConfigureAwait(false);

                if (request.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
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

    public async Task PublishEventAsync(string eventText)
    {
        var message = Encoding.UTF8.GetBytes($"EVENT: {eventText}\n");
        Subscriber[] currentSubscribers;

        lock (subscribersLock)
        {
            currentSubscribers = subscribers.ToArray();
        }

        foreach (var subscriber in currentSubscribers)
        {
            try
            {
                await subscriber.WriteLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                try
                {
                    await subscriber.Stream.WriteAsync(message.AsMemory(0, message.Length), cancellationTokenSource.Token).ConfigureAwait(false);
                }
                finally
                {
                    subscriber.WriteLock.Release();
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to write event to subscriber, removing subscriber.");
                RemoveSubscriber(subscriber);
            }
        }
    }

    private async Task SubscribeClientAsync(TcpClient client, NetworkStream networkStream, CancellationToken cancellationToken)
    {
        var subscriber = new Subscriber(client, networkStream);
        AddSubscriber(subscriber);

        try
        {
            var ackBytes = Encoding.UTF8.GetBytes("SUBSCRIBED\n");
            await networkStream.WriteAsync(ackBytes.AsMemory(0, ackBytes.Length), cancellationToken).ConfigureAwait(false);

            var buffer = new byte[256];
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (bytesRead <= 0)
                {
                    break;
                }

                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                if (request.Equals("QUIT", StringComparison.OrdinalIgnoreCase) || request.Equals("UNSUBSCRIBE", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested.
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Subscriber monitor error.");
            Console.WriteLine($"Subscriber monitor error: {ex.Message}");
        }
        finally
        {
            RemoveSubscriber(subscriber);
        }
    }

    private void AddSubscriber(Subscriber subscriber)
    {
        lock (subscribersLock)
        {
            subscribers.Add(subscriber);
        }
    }

    private void RemoveSubscriber(Subscriber subscriber)
    {
        lock (subscribersLock)
        {
            subscribers.Remove(subscriber);
        }

        try
        {
            subscriber.Client.Close();
        }
        catch
        {
        }
    }

    private sealed class Subscriber
    {
        public TcpClient Client { get; }
        public NetworkStream Stream { get; }
        public SemaphoreSlim WriteLock { get; } = new(1, 1);

        public Subscriber(TcpClient client, NetworkStream stream)
        {
            Client = client;
            Stream = stream;
        }
    }
}
