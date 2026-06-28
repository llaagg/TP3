using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Client;
using TP3.Interfaces;
using NodeType = TP3.Agent.Logic.Agent.Node;

public static class AgentInstance
{
    public static NodeType? Agent
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
