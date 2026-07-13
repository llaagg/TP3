using TP3.Interfaces;
namespace TP3.Service.Shell;

public class ShellService : BaseDirectoryNode, IService
{
    private State stateNode;
    private CancellationTokenSource? lifecycleCts;
    private readonly object listenerTasksLock = new();
    private readonly List<Task> listenerTasks = [];

    public ShellService()
        : base("shell")
    {
        this.stateNode = new State("state");
    }

    override public IEnumerable<INode>? Children => GetChildren();

    private List<INode> GetChildren() =>
    [
        this.stateNode,
        new BaseDirectoryNode("control", null,
        [
            new Ls(),
            new Cd(),
            new Sh(),
            new Create(this)
        ]),
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
