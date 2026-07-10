using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using TP3.Service.Remote;

namespace TP3.Protocol;

public class BaseControlCommandNode : INode
{
    public BaseControlCommandNode()
    {
        // short guid if empty
        this.Id = Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        this.Name = this.GetType().Name;
    }

    public string Id { get; }
    public string Name { get; }

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => null;

    public async Task<ITP3DataStream?> Get()
    {
        // Placeholder data stream for command nodes.
        // Real duplex command execution should be bound to transport session streams.
        var stream = new MemoryStream();
        return await Task.FromResult<ITP3DataStream?>(new TP3Stream(stream));
    }

    public Task Command(Stream duplex)
    {
        return Command(duplex, duplex);
    }

    public async Task Command(Stream input, Stream output)
    {
        using var reader = new StreamReader(input, leaveOpen: true);
        using var writer = new StreamWriter(output, leaveOpen: true)
        {
            AutoFlush = true
        };

        var hasArgsOverride = SupportsArgsMode();
        var hasStreamOverride = SupportsStreamMode();

        if (!hasArgsOverride && !hasStreamOverride)
        {
            await writer.WriteLineAsync("error command has no handlers");
            return;
        }

        // First line decides the command mode:
        // args <arg1> <arg2> ...
        // stream
        var header = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(header))
        {
            await writer.WriteLineAsync("error empty command header");
            return;
        }

        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mode = parts[0].Trim().ToLowerInvariant();

        if (mode == "args")
        {
            if (!hasArgsOverride)
            {
                await writer.WriteLineAsync("error args mode not supported");
                return;
            }

            var args = parts.Skip(1).ToArray();
            await HandleArgsCommand(args, writer);
            await writer.WriteLineAsync("ok done");
            return;
        }

        if (mode == "stream")
        {
            if (!hasStreamOverride)
            {
                await writer.WriteLineAsync("error stream mode not supported");
                return;
            }

            await HandleStreamCommand(input, output, writer);
            return;
        }

        // Auto mode makes one-handler commands easy:
        // - only args handler -> whole line treated as args payload
        // - only stream handler -> first line is treated as the beginning of stream payload
        if (hasArgsOverride && !hasStreamOverride)
        {
            var args = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            await HandleArgsCommand(args, writer);
            await writer.WriteLineAsync("ok done");
            return;
        }

        if (hasStreamOverride && !hasArgsOverride)
        {
            var prefix = System.Text.Encoding.UTF8.GetBytes(header + Environment.NewLine);
            using var prefixedInput = new PrefixStream(prefix, input);
            await HandleStreamCommand(prefixedInput, output, writer);
            return;
        }

        await writer.WriteLineAsync("error unsupported command mode (expected: args|stream)");
    }

    protected virtual Task HandleArgsCommand(string[] args, StreamWriter output)
    {
        return output.WriteLineAsync("error args mode not implemented");
    }

    protected virtual async Task HandleStreamCommand(Stream input, Stream output, StreamWriter control)
    {
        await control.WriteLineAsync("error stream mode not implemented");
    }

    private bool SupportsArgsMode()
    {
        var method = GetType().GetMethod(
            nameof(HandleArgsCommand),
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

        return method?.DeclaringType != typeof(BaseControlCommandNode);
    }

    private bool SupportsStreamMode()
    {
        var method = GetType().GetMethod(
            nameof(HandleStreamCommand),
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

        return method?.DeclaringType != typeof(BaseControlCommandNode);
    }
}
