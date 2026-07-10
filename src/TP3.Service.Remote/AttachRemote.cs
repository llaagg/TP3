using TP3.Protocol;

namespace TP3.Service.Remote;

internal class AttachRemote : BaseControlParamsArgsCommand
{
    protected override Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        using var writer = new StreamWriter(output, leaveOpen: true)
        {
            AutoFlush = true
        };

        if(string.IsNullOrEmpty(args?.FirstOrDefault()))
        {
            writer.WriteLine("AttachRemote command executed with no args");
            return Task.CompletedTask;
        }        

        writer.WriteLine("AttachRemote command executed with args: " + string.Join(", ", args));
        return Task.CompletedTask;
    }
}
