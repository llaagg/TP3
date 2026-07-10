
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Protocol;
using TP3.Messages;

/// <summary>
/// Used as a connection to another server via TCP
/// Should be a log running connection, and should be able to consume messages.
/// Should show status, and auto reconnect if the connection is lost. And die
/// if the connection is lost and cannot be re-established.
/// </summary>
public class TCPTP3RemoveClient
{
    public int Port { get; private set; }
    public string Host { get; private set; }
    private readonly ILogger? logger;
    private NetworkStream TcpStream;
    private TcpClient TcpClient;
    private string rootTag;

    public TCPTP3RemoveClient(
        ILogger? logger,
        string host, int port, int timeoutSeconds = 5)
    {
        this.logger = logger;
        this.Host = host;
        this.Port = port;
        this.TcpClient = new TcpClient
        {
            ReceiveTimeout = timeoutSeconds * 1000,
            SendTimeout = timeoutSeconds * 1000
        };
    }

    public async Task ConnectAsync()
    {
        this.logger?.LogInformation("Connecting to remote server {Host}:{Port}...", Host, Port);
        await this.TcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);
        this.TcpStream = this.TcpClient.GetStream();
    }

    public async Task TP3Attach()
    {
        string myName = "tcp-client@" + Environment.MachineName;
        this.logger?.LogInformation("Attaching to remote server with tag {Tag}...", myName);
        // now attach
        await SendMessageAsync(new TP3Message()
        {
            Tag = myName,
            AttachRequest = new TP3AttachRequest()
        }); 
        
        // wait for one response
        await foreach (var response in ListenAsync(CancellationToken.None).ConfigureAwait(false))
        {
            if(response.PayloadCase == TP3Message.PayloadOneofCase.AttachResponse)
            {
                logger?.LogInformation("Attached to remote server successfully.");
            }
            else if(response.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                logger?.LogError("Failed to attach to remote server: {ErrorMessage}", response.Error?.Message);

                throw new InvalidOperationException($"Failed to attach to remote server: {response.Error?.Message}");
            }

            this.rootTag = response.Tag;
            break; // exit after the first response
        }
        
    }

    public async Task SendMessageAsync(TP3Message message)
    {
        EnsureConnected();

        var packet = TP3Serializer.SerializeBytes(message);
        logger?.LogInformation("Sending message to IPC server: {Message}", message);
        await TcpStream!.WriteAsync(packet.AsMemory(0, packet.Length)).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<TP3Message> ListenAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        logger?.LogInformation("Listening for responses from remote server...");

        while (this.TcpClient.Client.Connected)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                logger?.LogInformation("Cancellation requested. Stopping listening for responses.");
                yield break;
            }
            
            TP3Message response;
            try
            {
                response = await TP3Serializer.ReadMessageAsync(TcpStream!, cancellationToken).ConfigureAwait(false);
            }
            catch (EndOfStreamException)
            {
                logger?.LogInformation("Remote server closed the connection.");
                break;
            }
            catch (IOException ex)
            {
                logger?.LogWarning(ex, "Remote server stream closed unexpectedly.");
                break;
            }
            logger?.LogInformation("Received response from remote server: {PayloadCase}", response.PayloadCase);
            yield return response;
        }
    }

    private void EnsureConnected()
    {
        if (TcpStream == null || !TcpClient.Connected)
        {
            throw new InvalidOperationException("Not connected to the server.");
        }
    }
}