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

    private static List<(string argument, string optionalParameter)> GetArguments(params string[]?args)
    {
        var result = new List<(string argument, string optionalParameter)>();
        // find all arguments starting with -- or - and thier respective paramester values
        
        for (int i = 0; i < (args?.Length ?? 0); i++)
        {
            var arg = args![i];
            if (arg.StartsWith("--") || arg.StartsWith("-"))
            {
                string? optionalParameter = null;
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--") && !args[i + 1].StartsWith("-"))
                {
                    optionalParameter = args[i + 1];
                    i++;
                }
                result.Add((arg, optionalParameter));
            }
        }

        return result;
    }

    protected abstract Task HandleParamsArgsCommand(Stream output, params string[]? args);
}
