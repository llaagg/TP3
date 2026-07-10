using TP3.Protocol;

namespace TP3.Service.Remote;

internal class AttachRemote : BaseControlCommand
{
    protected override async Task HandleStreamCommand(Stream input, Stream output, StreamWriter control)
    {
        await control.WriteLineAsync("ok attach stream started");

        // Demo behavior: pass bytes through. Derived commands can replace this with transforms.
        await input.CopyToAsync(output);
        await output.FlushAsync();
    }
}
