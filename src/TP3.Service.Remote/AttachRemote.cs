using TP3.Interfaces;
using TP3.Messages;
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

    public NodeType NodeType => NodeType.Command;

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

internal sealed class PrefixStream : Stream
{
    private readonly byte[] prefix;
    private readonly Stream inner;
    private int prefixOffset;

    public PrefixStream(byte[] prefix, Stream inner)
    {
        this.prefix = prefix;
        this.inner = inner;
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (prefixOffset < prefix.Length)
        {
            var remaining = prefix.Length - prefixOffset;
            var toCopy = Math.Min(count, remaining);
            Buffer.BlockCopy(prefix, prefixOffset, buffer, offset, toCopy);
            prefixOffset += toCopy;
            return toCopy;
        }

        return inner.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (prefixOffset < prefix.Length)
        {
            var remaining = prefix.Length - prefixOffset;
            var toCopy = Math.Min(buffer.Length, remaining);
            prefix.AsMemory(prefixOffset, toCopy).CopyTo(buffer);
            prefixOffset += toCopy;
            return toCopy;
        }

        return await inner.ReadAsync(buffer, cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (prefixOffset < prefix.Length)
        {
            var remaining = prefix.Length - prefixOffset;
            var toCopy = Math.Min(count, remaining);
            Buffer.BlockCopy(prefix, prefixOffset, buffer, offset, toCopy);
            prefixOffset += toCopy;
            return Task.FromResult(toCopy);
        }

        return inner.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}