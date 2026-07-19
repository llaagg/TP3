using TP3.Interfaces;
using TP3.Protocol;

namespace TP3.Service.FileSystem;

public class Meta : BaseControlParamsArgsCommand
{
    private readonly IService service;

    public Meta(IService service)
    {
        this.service = service;
    }

    protected override async Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        await using var writer = new StreamWriter(output, leaveOpen: true)
        {
            AutoFlush = true
        };
        if (args?.Length > 0)
        {
            // respond with descript of arguments that we need a path to node serpated with space
            await writer.WriteLineAsync("Command requires a path to the node, separated by space.");
            // flush and close and return
            await writer.FlushAsync();
            return;
        }   
        
        // let's find the node from params
        var node = this.service as INode;
        foreach (var arg in args ?? Array.Empty<string>())
        {
            node = node?.Children?.FirstOrDefault(c => c.Name == arg);
        }

        // get meta data for the node
        var meta = node?.GetMeta();

        if(meta!=null)
        {
            foreach (var m in meta.Properties)
            {
                await writer.WriteLineAsync($"{m.GetValue()}");
            }
        }
    }
}