using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Protocol;
using TP3.Messages;

namespace TP3.CLI;

internal sealed class IpcClient
{
    private readonly int ipcPort;
    private readonly ILogger logger;
    private TcpClient? tcpClient;
    private NetworkStream? stream;
    private readonly int waitForServer;

    public IpcClient(int ipcPort, ILogger logger, int waitForServer)
    {
        this.ipcPort = ipcPort;
        this.logger = logger;
        this.waitForServer = waitForServer;
    }

    public async Task ConnectAsync()
    {
        logger.LogInformation("Connecting to IPC server on port {IpcPort}...", ipcPort);

        if (tcpClient?.Connected == true)
        {
            logger.LogDebug("IPC client is already connected.");
            return;
        }

        tcpClient = new TcpClient();
        var connectTask = tcpClient.ConnectAsync(IPAddress.Loopback, ipcPort);
        if (await Task.WhenAny(connectTask, Task.Delay(waitForServer * 1000)) != connectTask)
        {
            throw new TimeoutException("Timed out waiting for IPC server to be ready.");
        }
        await connectTask.ConfigureAwait(false);
        stream = tcpClient.GetStream();

        logger.LogInformation("Connected to IPC server on port {IpcPort}.", ipcPort);
    }

    public async Task SendMessageAsync(string message)
    {
        EnsureConnected();

        var tp3Message = ParseMessage(message);
        var packet = TP3Serializer.SerializeBytes(tp3Message);

        logger.LogInformation("Sending TP3 message to IPC server: {Message}", tp3Message);
        await stream!.WriteAsync(packet.AsMemory(0, packet.Length)).ConfigureAwait(false);
    }

    public Task DisconnectAsync()
    {
        logger.LogInformation("Disconnecting from IPC server on port {IpcPort}...", ipcPort);

        stream?.Dispose();
        stream = null;

        tcpClient?.Close();
        tcpClient?.Dispose();
        tcpClient = null;

        return Task.CompletedTask;
    }

    internal async Task ListenAsync()
    {
        EnsureConnected();

        logger.LogInformation("Listening for responses from IPC server...");

        while (tcpClient?.Connected == true)
        {
            TP3Message response;
            try
            {
                response = await TP3Serializer.ReadMessageAsync(stream!, CancellationToken.None).ConfigureAwait(false);
            }
            catch (EndOfStreamException)
            {
                logger.LogInformation("IPC server closed the connection.");
                break;
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "IPC stream closed unexpectedly.");
                break;
            }

            logger.LogInformation("Received response from IPC server: {Response}", response);
        }
    }

    private void EnsureConnected()
    {
        if (tcpClient?.Connected != true || stream is null)
        {
            throw new InvalidOperationException("IPC client is not connected.");
        }
    }

    private static TP3Message ParseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new TP3Message(TP3Command.NONE);
        }

        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return new TP3Message(TP3Command.NONE);
        }

        if (!Enum.TryParse(parts[0], ignoreCase: true, out TP3Command command))
        {
            command = TP3Command.ECHO;
        }

        var pathSegments = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
        return new TP3Message(command, pathSegments);
    }
}