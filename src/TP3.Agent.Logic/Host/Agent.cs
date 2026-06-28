using TP3.Interfaces;

namespace TP3.Agent.Logic;

/// <summary>
/// Long time thread, listens for incoming connections and handles them.
/// Delivers trunk to the clients connected to it. (that should be other agents)
/// Connects to other agents and requests trunk from them.  
/// </summary>
public class Agent : IAgent
{
    public Agent()
    {
        this.T = new Trunk();
        this.MetaData = new MetaData()
        {
            new MetaDataItem { Name = "Symbol", Value = "💻" },
            new MetaDataItem { Name = "HostName", Value = System.Net.Dns.GetHostName() },
            new MetaDataItem { Name = "OS", Value = System.Runtime.InteropServices.RuntimeInformation.OSDescription },
        };
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
}
