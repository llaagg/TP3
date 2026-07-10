using TP3.Protocol;

namespace TP3.Service.Remote;

internal class AttachRemote : BaseControlCommandNode
{
    protected override Task HandleArgsCommand(string[] args, StreamWriter output)
    {
        if (args.Length == 0)
        {
            return output.WriteLineAsync("error attach args require remote id");
        }

        var remoteId = args[0];
        return output.WriteLineAsync($"ok attach requested for '{remoteId}'");
    }

    protected override async Task HandleStreamCommand(Stream input, Stream output, StreamWriter control)
    {
        await control.WriteLineAsync("ok attach stream started");

        // Demo behavior: pass bytes through. Derived commands can replace this with transforms.
        await input.CopyToAsync(output);
        await output.FlushAsync();
    }
}
