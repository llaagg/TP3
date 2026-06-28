using System.Linq;
using System.Text;
using TP3.Agent.Logic.Host;
using TP3.Interfaces;

namespace TP3.Agent.Logic;

/// <summary>
/// Long time thread, listens for incoming connections and handles them.
/// Delivers trunk to the clients connected to it. (that should be other agents)
/// Connects to other agents and requests trunk from them.  
/// </summary>
public class Agent : IAgent, IDisposable
{
    private readonly TCPTransport tcpTransport;

    public Agent()
    {
        this.T = new Trunk();
        this.MetaData = new MetaData()
        {
            new MetaDataItem { Name = "Symbol", Value = "💻" },
            new MetaDataItem { Name = "HostName", Value = System.Net.Dns.GetHostName() },
            new MetaDataItem { Name = "OS", Value = System.Runtime.InteropServices.RuntimeInformation.OSDescription },
        };

        tcpTransport = new TCPTransport(5000, BuildResponse);
    }
    
    public Trunk T
    {
        get; private set;
    }

    public static Agent Main(string[] args)
    {
        Console.WriteLine("Starting agent logic...");

        var agent = new Agent();
        return agent;
    } 

    public MetaData MetaData
    {
        get; private set;
    }

    public void Dispose()
    {
        tcpTransport.Dispose();
    }

    private string BuildResponse(string request)
    {
        return request.ToUpperInvariant() switch
        {
            "GET META" => string.Join("; ", MetaData.Select(item => $"{item.Name}={item.Value}")),
            "GET TRUNK" => T?.ToString() ?? "No trunk available",
            _ => $"ECHO: {request}",
        };
    }
}
