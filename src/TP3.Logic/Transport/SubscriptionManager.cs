using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TP3.Agent.Logic.Transport;

public sealed class SubscriptionManager : IDisposable
{
    private readonly List<Subscriber> subscribers = new();
    private readonly object subscribersLock = new();
    private readonly ILogger? logger;
    private bool disposed;

    public SubscriptionManager(ILogger? logger = null)
    {
        this.logger = logger;
    }

    public async Task PublishEventAsync(string eventText, CancellationToken cancellationToken)
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
                await subscriber.WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await subscriber.Stream.WriteAsync(message.AsMemory(0, message.Length), cancellationToken).ConfigureAwait(false);
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

    public async Task SubscribeClientAsync(TcpClient client, NetworkStream networkStream, CancellationToken cancellationToken)
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

    public void AddSubscriber(Subscriber subscriber)
    {
        lock (subscribersLock)
        {
            subscribers.Add(subscriber);
        }
    }

    public void RemoveSubscriber(Subscriber subscriber)
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

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        lock (subscribersLock)
        {
            foreach (var subscriber in subscribers.ToArray())
            {
                try
                {
                    subscriber.Client.Close();
                }
                catch
                {
                }
            }

            subscribers.Clear();
        }
    }
}
