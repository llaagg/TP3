using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Service.Remote;

internal class AttachRemote : BaseControlCommandNode
{
}

public class BaseControlCommandNode : INode
{
    public BaseControlCommandNode()
    {
        // short guid if empty
        this.Id = Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        this.Name = this.GetType().Name;
    }

    public string? Id { get; }
    public string? Name { get; }

    public NodeType NodeType => NodeType.Command;

    public IEnumerable<INode>? Children => null;

    public async Task<ITP3DataStream?> Get()
    {
        // it can read from stream and it will respond in stream
        var stream = new MemoryStream();
        return await Task.FromResult<ITP3DataStream?>(new TP3Stream(stream));
    }

    public async Task<string> Command(string command, string? args = null)
    {
        // implement command handling logic here
        // await Task.Yield();
        if(command == "attach")
        {
            // handle attach command
            return "Attach command executed successfully.";
        }
        return $"Executed command: {command} with args: {args}";
    }
}