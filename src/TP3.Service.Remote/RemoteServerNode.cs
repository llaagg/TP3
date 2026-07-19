using Microsoft.Extensions.Logging;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol.Base;

namespace TP3.Service.Remote;

internal sealed class RemoteConnectionNode : BaseDirectoryNode, IDisposable
{
    private readonly RemoteDirectoryNode root;
    private readonly RemoteTcpClient client;
    private readonly string rootTag;

    public RemoteConnectionNode(RemoteTcpClient client, string tag, string displayName) : base(displayName, tag)
    {
        this.client = client;
        this.rootTag = tag;
        this.root = new RemoteDirectoryNode("root", client, tag, Array.Empty<string>());
    }

    public override IEnumerable<INode>? Children => new INode[] { this.root };

    public void Dispose()
    {
        try
        {
            this.client.ClunkAsync(this.rootTag).GetAwaiter().GetResult();
        }
        catch
        {
        }

        this.client.Dispose();
    }
}

internal sealed class RemoteDirectoryNode : BaseDirectoryNode
{
    private readonly RemoteTcpClient client;
    private readonly string rootTag;
    private readonly string[] path;
    private readonly object sync = new();
    private List<INode>? children;

    public RemoteDirectoryNode(string name, RemoteTcpClient client, string rootTag, string[] path) : base(name)
    {
        this.client = client;
        this.rootTag = rootTag;
        this.path = path;
    }

    public override IEnumerable<INode>? Children
    {
        get
        {
            EnsureLoaded();
            return children;
        }
    }

    private void EnsureLoaded()
    {
        if (children is not null)
        {
            return;
        }

        lock (sync)
        {
            if (children is not null)
            {
                return;
            }

            children = LoadChildren();
        }
    }

    private List<INode> LoadChildren()
    {
        var tempTag = client.NewTag();
        try
        {
            var walkResponse = client.WalkAsync(rootTag, tempTag, path).GetAwaiter().GetResult();
            if (walkResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException(walkResponse.Error?.Message ?? "Failed to walk remote directory.");
            }

            var openResponse = client.OpenAsync(tempTag).GetAwaiter().GetResult();
            if (openResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException(openResponse.Error?.Message ?? "Failed to open remote directory.");
            }

            var maxBytes = openResponse.OpenResponse.Iounit == 0 ? 16 * 1024 : openResponse.OpenResponse.Iounit;
            var childrenPayload = client.ReadDirectoryAsync(tempTag, maxBytes).GetAwaiter().GetResult();

            var result = new List<INode>();
            foreach (var child in childrenPayload)
            {
                var childPath = path.Append(child.Name).ToArray();
                if (child.Info.NodeType == NodeType.Directory)
                {
                    result.Add(new RemoteDirectoryNode(child.Name, client, rootTag, childPath));
                }
                else
                {
                    result.Add(new RemoteFileNode(child.Name, childPath, client, rootTag));
                }
            }

            return result;
        }
        finally
        {
            try
            {
                client.ClunkAsync(tempTag).GetAwaiter().GetResult();
            }
            catch
            {
            }
        }
    }
}

internal sealed class RemoteFileNode : INode
{
    private readonly RemoteTcpClient client;
    private readonly string rootTag;
    private readonly string[] path;
    private readonly string name;

    public RemoteFileNode(string name, string[] path, RemoteTcpClient client, string rootTag)
    {
        this.name = name;
        this.client = client;
        this.rootTag = rootTag;
        this.path = path;
    }

    public string Id => this.name;

    public string Name => this.name;

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => null;

    public ulong Length => 0;

    public async Task<ITP3DataStream?> Get()
    {
        var fileStream = new RemoteFileDataStream(client, rootTag, path);
        await fileStream.Open().ConfigureAwait(false);
        return fileStream;
    }
}

internal sealed class RemoteFileDataStream : ITP3DataStream
{
    private readonly RemoteTcpClient client;
    private readonly string rootTag;
    private readonly string[] path;
    private string? openTag;
    private uint iounit = 16 * 1024;

    public RemoteFileDataStream(RemoteTcpClient client, string rootTag, string[] path)
    {
        this.client = client;
        this.rootTag = rootTag;
        this.path = path;
    }

    public uint Iounit => iounit;

    public ulong Position => 0;

    public async Task Open()
    {
        openTag = client.NewTag();
        var walkResponse = await client.WalkAsync(rootTag, openTag, path).ConfigureAwait(false);
        if (walkResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException(walkResponse.Error?.Message ?? "Failed to walk remote file.");
        }

        var openResponse = await client.OpenAsync(openTag).ConfigureAwait(false);
        if (openResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException(openResponse.Error?.Message ?? "Failed to open remote file.");
        }

        iounit = openResponse.OpenResponse.Iounit == 0 ? iounit : openResponse.OpenResponse.Iounit;
    }

    public async Task<byte[]> Read(ulong offset, ulong maxCount)
    {
        EnsureOpen();

        var response = await client.ReadAsync(openTag!, offset, (uint)(maxCount == 0 ? iounit : Math.Min(maxCount, uint.MaxValue))).ConfigureAwait(false);
        if (response.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException(response.Error?.Message ?? "Remote read failed.");
        }

        return response.ReadResponse.Data.ToByteArray();
    }

    public async Task<ulong> Write(ulong offset, byte[] data)
    {
        EnsureOpen();

        var response = await client.WriteAsync(openTag!, offset, data).ConfigureAwait(false);
        if (response.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException(response.Error?.Message ?? "Remote write failed.");
        }

        return response.WriteResponse.Count;
    }

    public void Close()
    {
        if (openTag is null)
        {
            return;
        }

        try
        {
            client.ClunkAsync(openTag).GetAwaiter().GetResult();
        }
        catch
        {
        }
        finally
        {
            openTag = null;
        }
    }

    private void EnsureOpen()
    {
        if (openTag is null)
        {
            throw new InvalidOperationException("Remote file stream is not open.");
        }
    }
}