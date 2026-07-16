using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Service.PS;

public class Screen : INode
{
    public Screen()
    {
    }

    public string Id => "0";

    public string Name => "screen.png";

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => null;

    public ulong Length
    {
        get
        {            
            return 0;
        }
    }

    public async Task<ITP3DataStream?> Get()
    {
        // var bytes = WindowsScreenCapture.GetPng();
        // return TP3Stream.CreateFromBytes(bytes);
        return new TP3LiveStream();
    }
}


public class TP3LiveStream : ITP3DataStream
{
    public uint Iounit => 0;

    public ulong Position => _position;

    private ulong _position;

    public void Close()
    {
    }

    public async Task Open()
    {
        this._position = 0;
    }

    public Task<byte[]> Read(ulong offset, ulong maxCount)
    {
        if(_position == 0)
        {
            var bytes = WindowsScreenCapture.GetPng();
            _position += (ulong)bytes.Length;
            return Task.FromResult(bytes);
        }
        else
        {
            return Task.FromResult(Array.Empty<byte>());
        }
    }

    public async Task<ulong> Write(ulong offset, byte[] data)
    {
        return 0UL;
    }
}
