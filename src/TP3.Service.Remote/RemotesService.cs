using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Service.Remote;

public class RemotesService : BaseDirectoryNode, IService
{
    public RemotesService(ILogger logger) : base("remotes")
    {
        this.State = new RemoteNodes();
        this.Control = new ControlNodes(this);
        this.logger = logger;
    }

    public async Task Init(IAgent me)
    {

    }

    public async Task Start()
    {
    }

    public async Task Stop()
    {
    }

    public void Dispose()
    {
        foreach (var connection in this.State.Children ?? Array.Empty<INode>())
        {
            if (connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public async Task<AttachRemoteResult> AttachTcpRemote(string host, int port)
    {
        var result = new AttachRemoteResult();
        
        try
        {
            var client = new RemoteTcpClient(this.logger, host, port);
            await client.ConnectAsync().ConfigureAwait(false);

            this.State.AddConnection(new RemoteConnectionNode(client, client.RootTag, $"{host}:{port}"));

            result.Success = true;
            result.Message = $"Connected successfully to {host}:{port}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Failed to connect: {ex.Message}";
        }
        return result;
    }

    override public IEnumerable<INode>? Children => new List<INode>() { State, Control };

    public RemoteNodes State { get; }
    public ControlNodes Control { get; }

    private ILogger logger;
}
