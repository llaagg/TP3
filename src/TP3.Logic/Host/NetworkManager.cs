using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public class NetworkManager : INetworkManager, IDisposable
{
    private readonly ILogger? logger;
    public readonly IRouter router;

    private NetworkSessions networkSessions = new NetworkSessions();
    private List<TP3Transport> transports = new List<TP3Transport>();

    public INetworkSessions NetworkSessions => networkSessions;

    public NetworkManager(IRouter router, ILogger? logger = null)
    {
        this.logger = logger;

        this.router = router;
    }


    public async Task Init()
    {
        if (this.transports != null)
        {
            // check if tags are uniq in tranbsports
            ValidateTag();

            foreach (var t in transports)
            {
                await t.Init(this, router);
            }
        }
    }

    private void ValidateTag()
    {
        var tags = transports.Select(t => t.TransportTag).ToList();
        if (tags.Count != tags.Distinct().Count())
        {
            throw new Exception("Transport tags are not unique.");
        }
    }


    public void Dispose()
    {
        logger?.LogInformation("Disposing agent host.");
    }

    public async Task AddTransport(INetworkTransport transport)
    {
        this.logger?.LogInformation("Adding transport: {TransportType}", transport.GetType().Name);
        var tp3Transport = new TP3Transport(this.logger, transport);
        if (this.transports.Any(t => t.TransportTag == tp3Transport.TransportTag))
        {
            throw new Exception($"Transport with tag '{tp3Transport.TransportTag}' already exists.");
        }

        this.transports.Add(tp3Transport);
        await tp3Transport.Init(this, router);
    }
}
