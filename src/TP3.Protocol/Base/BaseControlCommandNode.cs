using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Base;

namespace TP3.Protocol;

public class BaseControlCommand : INode
{
    private const ulong DefaultMaxCount = 16 * 1024;

    public BaseControlCommand()
    {
        // short guid if empty
        this.Id = Guid.NewGuid().ToString().Substring(0, 8);
        // type name if empty
        this.Name = BaseDirectoryNode.NameCreator(this.GetType());
    }

    public string Id { get; }
    public string Name { get; }

    public NodeType NodeType => NodeType.Command;

    public IEnumerable<INode>? Children => null;

    public ulong Length => 0;

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(new MemeoryCachedCommandDataStream(this));
    }

    public Task Command(Stream duplex)
    {
        return Command(duplex, duplex);
    }

    public async Task Command(Stream input, Stream output)
    {
        await HandleStreamCommand(input, output);
    }

    protected virtual async Task HandleStreamCommand(Stream input, Stream output)
    {
        using var writer = new StreamWriter(output, leaveOpen: true)
        {
            AutoFlush = true
        };
        await writer.WriteLineAsync("not implemented");
    }

    private sealed class MemeoryCachedCommandDataStream : ITP3DataStream
    {
        private readonly BaseControlCommand command;
        private readonly MemoryStream commandInput = new MemoryStream();
        private MemoryStream? commandOutput;
        private bool commandExecuted;

        public MemeoryCachedCommandDataStream(BaseControlCommand command)
        {
            this.command = command;
        }

        public uint Iounit => 0;

        public ulong Position => (ulong)(commandOutput?.Position ?? 0);

        public Task Open()
        {
            return Task.CompletedTask;
        }

        public async Task<byte[]> Read(ulong offset, ulong maxCount)
        {
            await EnsureExecuted();

            var output = commandOutput!;
            if (offset != (ulong)output.Position)
            {
                output.Seek((long)offset, SeekOrigin.Begin);
            }

            if (maxCount == 0)
            {
                maxCount = DefaultMaxCount;
            }

            var buffer = new byte[maxCount];
            var bytesRead = await output.ReadAsync(buffer, 0, (int)maxCount);
            if (bytesRead < (int)maxCount)
            {
                Array.Resize(ref buffer, bytesRead);
            }

            return buffer;
        }

        public async Task<ulong> Write(ulong offset, byte[] data)
        {
            if (commandExecuted)
            {
                throw new InvalidOperationException("Cannot write to command stream after command execution has started.");
            }

            if (offset != (ulong)commandInput.Position)
            {
                commandInput.Seek((long)offset, SeekOrigin.Begin);
            }

            await commandInput.WriteAsync(data, 0, data.Length);
            return (ulong)data.Length;
        }

        public void Close()
        {
            commandInput.Dispose();
            commandOutput?.Dispose();
        }

        private async Task EnsureExecuted()
        {
            if (commandExecuted)
            {
                return;
            }

            commandInput.Seek(0, SeekOrigin.Begin);
            commandOutput = new MemoryStream();
            await command.Command(commandInput, commandOutput);
            commandOutput.Seek(0, SeekOrigin.Begin);
            commandExecuted = true;
        }
    }
}
