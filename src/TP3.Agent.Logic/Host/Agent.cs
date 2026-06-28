using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Agent.Logic;

/// <summary>
/// Long time thread, listens for incoming connections and handles them.
/// Delivers trunk to the clients connected to it. (that should be other agents)
/// Connects to other agents and requests trunk from them.  
/// </summary>
public class Agent : IAgent, IDisposable
{
    private readonly ILogger? logger;

    public Agent(ILogger? logger = null)
    {
        this.logger = logger;
        this.T = new Trunk();
        this.MetaData = new MetaData()
        {
            new MetaDataItem { Name = "Symbol", Value = "💻" },
            new MetaDataItem { Name = "HostName", Value = System.Net.Dns.GetHostName() },
            new MetaDataItem { Name = "OS", Value = System.Runtime.InteropServices.RuntimeInformation.OSDescription },
        };

        logger?.LogInformation("Initializing agent logic.");
    }
    
    public Trunk T
    {
        get; private set;
    }

    public MetaData MetaData
    {
        get; private set;
    }

    public virtual void Dispose()
    {
        // Agent logic has no transport of its own.
    }

}
