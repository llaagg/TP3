using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Service.PS;

public class Screen : INode
{
    public Screen(nint handle, bool isPrimary, int width, int height)
    {
        Handle = handle;
        IsPrimary = isPrimary;
        Width = width;
        Height = height;
    }

    public nint Handle { get; }
    public bool IsPrimary { get; }
    public int Width { get; }
    public int Height { get; }

    public string Id => "0";

    public string Name => $"{Handle}.png";

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
        return new TP3LiveStream(this.Handle);
    }
}


public class TP3LiveStream : ITP3DataStream
{
    private nint _handle;

    public TP3LiveStream(nint handle)
    {
        _handle = handle;
    }
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
            var bytes = WindowsScreenCapture.GetPng(_handle);
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
