using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Agent;

/// <summary>
/// Long time thread, listens for incoming connections and handles them.
/// Delivers trunk to the clients connected to it. (that should be other agents)
/// Connects to other agents and requests trunk from them.  
/// </summary>
public class Agent : IAgent
{
    private readonly ILogger? logger;

    public Agent(ILogger? logger = null)
    {
        this.logger = logger;

        logger?.LogInformation("Initializing agent logic.");
    }

    public INode T
    {
        get
        {
            return new Trunk(this.Services);
        }
    }

    public List<IService> Services { get; private set; } = new List<IService>();

    public async Task AddService(IService service)
    {
        try
        {
            await service.Init(this);
            this.Services.Add(service);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize service: {service.GetType().Name}", ex);
        }
    }

    public virtual void Dispose()
    {
        // Agent logic has no transport of its own.
    }

    public string HandleRequest(TP3Message request)
    {
        return request.Command switch
        {
            TP3Command.LIST => 
                string.Join(",", this.Services.Select(s => s.GetType().Name)),
            
            TP3Command.ECHO => $"ECHO: {request}",
            TP3Command.HI => $"HI,I am {request.Target}.",
        };
    }
}
