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
        return TP3Stream.CreateFromBytes(WindowsScreenCapture.GetPng());
    }
}
