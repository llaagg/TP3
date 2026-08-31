using System.Net;
using System.Net.Sockets;
using TP3.Agent.Logic.Protocol;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Service.Remote;

internal sealed class RemoteTcpClient : IDisposable
{
    private readonly ILogger? logger;
    private readonly TcpClient tcpClient;
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private readonly string rootTag;
    private NetworkStream? stream;
    private bool connected;

    public RemoteTcpClient(ILogger? logger, string host, int port)
    {
        this.logger = logger;
        this.Host = host;
        this.Port = port;
        this.rootTag = $"remote-{Guid.NewGuid():N}"[..16];
        this.tcpClient = new TcpClient();
    }

    public string Host { get; }

    public int Port { get; }

    public async Task ConnectAsync()
    {
        if (connected)
        {
            return;
        }

        logger?.LogInformation("Connecting to remote server {Host}:{Port}...", Host, Port);
        await tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);
        stream = tcpClient.GetStream();
        connected = true;

        await AttachAsync().ConfigureAwait(false);
    }

    public string RootTag => rootTag;

    public async Task AttachAsync()
    {
        await SendAndWaitOneAsync(new TP3Message
        {
            Tag = rootTag,
            AttachRequest = new TP3AttachRequest()
        }).ConfigureAwait(false);
    }

    public async Task<TP3Message> WalkAsync(string tag, string newTag, params string[] path)
    {
        var request = new TP3Message
        {
            Tag = tag,
            WalkRequest = new TP3WalkRequest
            {
                NewTag = newTag
            }
        };
        request.WalkRequest.Path.Add(path);

        return await SendAndWaitOneAsync(request).ConfigureAwait(false);
    }

    public async Task<TP3Message> OpenAsync(string tag)
    {
        return await SendAndWaitOneAsync(new TP3Message
        {
            Tag = tag,
            OpenRequest = new TP3OpenRequest()
        }).ConfigureAwait(false);
    }

    public async Task<TP3Message> ReadAsync(string tag, ulong offset, uint maxBytes)
    {
        return await SendAndWaitOneAsync(new TP3Message
        {
            Tag = tag,
            ReadRequest = new TP3ReadRequest
            {
                Offset = offset,
                MaxBytes = maxBytes
            }
        }).ConfigureAwait(false);
    }

    public async Task<TP3Message> WriteAsync(string tag, ulong offset, byte[] data)
    {
        return await SendAndWaitOneAsync(new TP3Message
        {
            Tag = tag,
            WriteRequest = new TP3WriteRequest
            {
                Offset = offset,
                Data = Google.Protobuf.ByteString.CopyFrom(data)
            }
        }).ConfigureAwait(false);
    }

    public async Task<TP3Message> ClunkAsync(string tag)
    {
        return await SendAndWaitOneAsync(new TP3Message
        {
            Tag = tag,
            ClunkRequest = new TP3ClunkRequest()
        }).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TP3StatPayload>> ReadDirectoryAsync(string tag, uint maxBytes)
    {
        var offset = 0UL;
        var result = new List<TP3StatPayload>();

        while (true)
        {
            var response = await ReadAsync(tag, offset, maxBytes).ConfigureAwait(false);
            if (response.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException(response.Error?.Message ?? "Remote read failed.");
            }

            var bytes = response.ReadResponse.Data.ToByteArray();
            if (bytes.Length == 0)
            {
                break;
            }

            using var memory = new MemoryStream(bytes);
            result.AddRange(TP3StatPayloadExtensions.Deserilize(memory));

            if (bytes.Length < maxBytes)
            {
                break;
            }

            offset += (ulong)bytes.Length;
        }

        return result;
    }

    public string NewTag()
    {
        return $"remote-{Guid.NewGuid():N}"[..16];
    }

    private async Task<TP3Message> SendAndWaitOneAsync(TP3Message request)
    {
        EnsureConnected();

        await sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var packet = TP3Serializer.SerializeBytes(request);
            await stream!.WriteAsync(packet.AsMemory(0, packet.Length)).ConfigureAwait(false);

            while (true)
            {
                var response = await TP3Serializer.ReadMessageAsync(stream!, CancellationToken.None).ConfigureAwait(false);
                if (response.Tag == request.Tag || response.Tag == request.WalkRequest?.NewTag)
                {
                    return response;
                }
            }
        }
        finally
        {
            sendLock.Release();
        }
    }

    private void EnsureConnected()
    {
        if (!connected || stream is null)
        {
            throw new InvalidOperationException("Remote TCP client is not connected.");
        }
    }

    public void Dispose()
    {
        stream?.Dispose();
        tcpClient.Dispose();
        sendLock.Dispose();
    }
}