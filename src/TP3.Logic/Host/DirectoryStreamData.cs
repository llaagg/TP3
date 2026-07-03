using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Agent.Logic.Transport;

/// <summary>
/// Lazily converts an <see cref="INode"/> directory's children into read bytes for TP3 transport.
/// </summary>
public class DirectoryStreamData : ITP3DataStream
{
    private readonly INode node;

    public DirectoryStreamData(INode node)
    {
        this.node = node;
    }

    public uint Iounit => 0;

    private ulong _position = 0;
    private IEnumerator<INode>? enumerator = null;

    public ulong Position => _position;

    public Task Open()
    {
        if (node.NodeType != NodeType.Directory)
        {
            throw new InvalidOperationException("Cannot open a non-directory node as a directory stream.");
        }
        this.enumerator = node.Children?.GetEnumerator()!;

        return Task.CompletedTask;
    }


    ulong defaultMaxCount = 16 * 1024; // default max count

    public async Task<byte[]> Read(ulong offset, ulong maxCount)
    {
        if(maxCount == 0)
        {
            maxCount = defaultMaxCount; 
        }

        // i will try to put here serilizez folders from node children and return them as TP3Stat one after another one,
        // i will store enumerator over children and procezzed everytime some time will ask to read
        // i will store position and check if offest is in  the same place as postion if not i will reset enumerator and start from begining and skip to offset
        if (node.NodeType != NodeType.Directory)
        {
            throw new InvalidOperationException("Cannot read from a non-directory node as a directory stream.");
        }

        var children = node.Children;
        if(node.Children == null)
        {
            return new byte[0];
        }

        if(offset != _position)
        {
            // reset enumerator and skip to offset
            _position = 0;
            this.enumerator = children!.GetEnumerator();
        }
        
        List<byte> result = new List<byte>();
        while (Convert.ToUInt64(result.Count) < maxCount)
        {
            if (enumerator!.MoveNext())
            {
                var child = enumerator.Current;
                var stat = new TP3StatPayload(child);
                var bytes = stat.Serialize();
                result.AddRange(bytes);
                _position++;
            }
            else
            {
                break;
            }
        }


        return result.ToArray();
    }
}
