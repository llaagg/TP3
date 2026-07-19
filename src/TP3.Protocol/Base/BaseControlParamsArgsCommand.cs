namespace TP3.Protocol;

public abstract class BaseControlParamsArgsCommand : BaseControlCommand
{
    protected override async Task HandleStreamCommand(Stream input, Stream output)
    {
        using var reader = new StreamReader(input, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        var args = ParseArgs(payload);

        await HandleParamsArgsCommand(output, args);
    }

    protected virtual string[] ParseArgs(string payload)
    {
        return payload
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    protected abstract Task HandleParamsArgsCommand(Stream output, params string[]? args);
}
