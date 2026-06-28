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
            return new LocalConnection(Agent);
        }
    }
}
