using TP3.Interfaces;
using TP3.Protocol;
using System.Text;
namespace TP3.Service.Shell;

public class ShellService : BaseDirectoryNode, IService
{
    private State stateNode;
    private BaseDirectoryNode controlNode;
    private readonly Dictionary<string, BaseControlCommand> controlCommands;
    private CancellationTokenSource? lifecycleCts;
    private readonly object listenerTasksLock = new();
    private readonly List<Task> listenerTasks = [];

    public ShellService()
        : base("shell")
    {
        this.stateNode = new State("state");

        var commands = new BaseControlCommand[]
        {
            new Ls(),
            new Cd(),
            new Sh(),
            new Create(this)
        };

        this.controlNode = new BaseDirectoryNode("control", null, commands.Cast<INode>().ToList());
        this.controlCommands = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    override public IEnumerable<INode>? Children => GetChildren();

    private List<INode> GetChildren() =>
    [
        this.stateNode,
        this.controlNode,
    ];

    public void Dispose()
    {
        var cts = this.lifecycleCts;
        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch
        {
            // Ignore dispose-time cancellation failures.
        }
        finally
        {
            cts.Dispose();
            this.lifecycleCts = null;
        }
    }

    public Task Init(IAgent me)
    {
        return Task.CompletedTask;
    }

    public Task Start()
    {
        EnsureLifecycleCts();
        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        var cts = this.lifecycleCts;
        if (cts is null)
        {
            return;
        }

        cts.Cancel();

        Task[] listenersSnapshot;
        lock (this.listenerTasksLock)
        {
            listenersSnapshot = this.listenerTasks.ToArray();
        }

        try
        {
            await Task.WhenAll(listenersSnapshot);
        }
        catch (OperationCanceledException)
        {
            // Expected when listeners are canceled by Stop().
        }
        finally
        {
            cts.Dispose();
            this.lifecycleCts = null;
        }
    }

    public async Task<int> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return 0;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var tokens = command
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return 0;
        }

        var commandKey = NormalizeCommandName(tokens[0]);
        if (!this.controlCommands.TryGetValue(commandKey, out var tp3Command))
        {
            return 127;
        }

        var payload = string.Join(' ', tokens.Skip(1));
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        await using var output = new MemoryStream();
        await tp3Command.Command(input, output);
        return 0;
    }

    private static string NormalizeCommandName(string raw)
    {
        var command = raw.Trim();
        if (command.StartsWith('/'))
        {
            var segments = command.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length > 0)
            {
                command = segments[^1];
            }
        }

        var dotIndex = command.LastIndexOf('.');
        if (dotIndex >= 0 && dotIndex < command.Length - 1)
        {
            command = command[(dotIndex + 1)..];
        }

        return command;
    }

    public Task<string> AddNewShell()
    {
        // we can connect to those streams and use tham as cli terminal
        // we will have some screen buffer and all the nice things
        string shortGuid = Guid.NewGuid().ToString().Substring(0, 8);
        var terminalName = $"terminal-{shortGuid}";
        var terminalNode = new Shell(terminalName);
        this.stateNode.AddChild(terminalNode);

        var token = EnsureLifecycleCts().Token;
        var listenerTask = this.StartListeningToInStream(terminalNode, token);
        TrackListener(listenerTask);

        return Task.FromResult(terminalName);
    }

    private CancellationTokenSource EnsureLifecycleCts()
    {
        if (this.lifecycleCts is null || this.lifecycleCts.IsCancellationRequested)
        {
            this.lifecycleCts?.Dispose();
            this.lifecycleCts = new CancellationTokenSource();
        }

        return this.lifecycleCts;
    }

    private void TrackListener(Task listenerTask)
    {
        lock (this.listenerTasksLock)
        {
            this.listenerTasks.Add(listenerTask);
        }

        _ = listenerTask.ContinueWith(_ =>
        {
            lock (this.listenerTasksLock)
            {
                this.listenerTasks.Remove(listenerTask);
            }
        }, TaskScheduler.Default);
    }

    private async Task StartListeningToInStream(Shell terminalNode, CancellationToken cancellationToken)
    {
        // Read bytes from terminal input and forward them to terminal output.
        var inStream = terminalNode.GetInReadStream();
        var outStream = terminalNode.GetOutWriteStream();

        var buffer = new byte[1024];
        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead;
            try
            {
                bytesRead = await inStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (bytesRead == 0)
            {
                // Stream was completed by writer.
                break;
            }

            await outStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            await outStream.FlushAsync(cancellationToken);
        }
    }
}
