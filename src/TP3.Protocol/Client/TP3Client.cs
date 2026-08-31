using System.Net;
using System.Net.Sockets;
using System.Text;
using TP3.Agent.Logic.Protocol;
using TP3.Messages;

namespace TP3.Protocol.Client;

public class TP3Client
{
    private int ipcPort = 5001;
    private readonly ILogger? logger;
    private TcpClient? tcpClient;
    private NetworkStream? stream;
    private readonly int waitForServer;

    public TP3Client(int ipcPort = 5001, ILogger? logger=null, int waitForServer=0)
    {
        this.ipcPort = ipcPort;
        this.logger = logger;
        this.waitForServer = waitForServer;
    }

    public int IpcPort { get => ipcPort; set => ipcPort = value; }

    public async Task ConnectAsync()
    {
        logger?.LogInformation("Connecting to IPC server on port {IpcPort}...", IpcPort);

        if (tcpClient?.Connected == true)
        {
            logger?.LogDebug("IPC client is already connected.");
            return;
        }

        tcpClient = new TcpClient();
        var connectTask = tcpClient.ConnectAsync(IPAddress.Loopback, ipcPort);
        if(waitForServer > 0)
        {
            if (await Task.WhenAny(connectTask, Task.Delay(waitForServer * 1000)) != connectTask)
            {
                throw new TimeoutException("Timed out waiting for IPC server to be ready.");
            }
        }else
        {
            await connectTask.ConfigureAwait(false);
        }
        await connectTask.ConfigureAwait(false);
        stream = tcpClient.GetStream();

        logger?.LogInformation("Connected to IPC server on port {IpcPort}.", ipcPort);
    }

    public async Task SendMessageAsync(TP3Message message)
    {
        EnsureConnected();

        var packet = TP3Serializer.SerializeBytes(message);
        logger?.LogInformation("Sending message to IPC server: {Message}", message);
        await stream!.WriteAsync(packet.AsMemory(0, packet.Length)).ConfigureAwait(false);
        
    }

    public Task DisconnectAsync()
    {
        logger?.LogInformation("Disconnecting from IPC server on port {IpcPort}...", ipcPort);

        stream?.Dispose();
        stream = null;

        tcpClient?.Close();
        tcpClient?.Dispose();
        tcpClient = null;

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<TP3Message> ListenAsync()
    {
        EnsureConnected();

        logger?.LogInformation("Listening for responses from IPC server...");

        while (tcpClient?.Connected == true)
        {
            TP3Message response;
            try
            {
                response = await TP3Serializer.ReadMessageAsync(stream!, CancellationToken.None).ConfigureAwait(false);
            }
            catch (EndOfStreamException)
            {
                logger?.LogInformation("IPC server closed the connection.");
                break;
            }
            catch (IOException ex)
            {
                logger?.LogWarning(ex, "IPC stream closed unexpectedly.");
                break;
            }

            logger?.LogInformation("Received response from IPC server: {PayloadCase}", response.PayloadCase);
            yield return response;
        }
    }

    private void EnsureConnected()
    {
        if (tcpClient?.Connected != true || stream is null)
        {
            throw new InvalidOperationException("IPC client is not connected.");
        }
    }

}