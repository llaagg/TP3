using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Client;
using TP3.Interfaces;

public static class AgentInstance
{
    public static TP3.Agent.Logic.Agent Agent
    {
        get; set;
    }

    public static IConnection Connection
    {
        get
        {
            return ConnectionHelper.ConnectTcp("localhost", 5000, Logger as ILogger<Connection>);
        }
    }

    public static ILogger? Logger { get; set; }
}
