using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TP3.Agent.Logic.Host;

public sealed class IpcTransport : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly TcpListener listener;
    private readonly Func<string, string> requestHandler;
    private readonly ILogger? logger;
    private bool disposed;

    public IpcTransport(int port, Func<string, string> requestHandler, ILogger? logger = null)
    {
        this.requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        this.logger = logger;
        listener = new TcpListener(IPAddress.Loopback, port);
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
            logger?.LogInformation("IPC listener started on port {Port}", port);
            Console.WriteLine($"IPC listener started on port {port}");
            _ = Task.Run(() => AcceptLoopAsync(cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to start IPC listener.");
            Console.WriteLine($"Failed to start IPC listener: {ex.Message}");
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

        try
        {
            logger?.LogInformation("IPC client connected: {ClientEndpoint}", client.Client.RemoteEndPoint);
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var request = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (request is null)
                {
                    break;
                }

                var trimmed = request.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                var response = requestHandler(trimmed);
                await writer.WriteLineAsync(response).ConfigureAwait(false);

                if (trimmed.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
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
            client.Close();
        }
    }
}
