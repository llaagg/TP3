using TP3.Interfaces;

namespace TP3.Protocol.MetaData;

public class MetaGet : BaseControlParamsArgsCommand
{
    private readonly IService service;

    public MetaGet(IService service)
    {
        this.service = service;
    }

    protected override async Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        await using var writer = new StreamWriter(output, leaveOpen: true)
        {
            AutoFlush = true
        };

        if (args?.Length == 0)
        {
            // respond with descript of arguments that we need a path to node serpated with space
            await writer.WriteLineAsync("Command requires property name and a path to the node, separated by space. for ex.: state drives c:\\ ");
            // flush and close and return
            await writer.FlushAsync();
            return;
        }   
        
        var propertyName = args[0];
        // let's find the node from params
        var node = this.service as INode;
        foreach (var arg in args.Skip(1))
        {
            node = node?.Children?.FirstOrDefault(c => c.Name == arg);
        }

        // get meta data for the node
        var meta = node?.GetMeta();

        if(meta!=null)
        {
            var m = meta.Properties.FirstOrDefault(p => p.Name == propertyName);
            if (m != null)
            {
                await writer.WriteLineAsync($"{m.GetValue()}");
                return;
            }
        }
    }
}

