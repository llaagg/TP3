namespace TP3.Agent.Logic;

/// <summary>
/// Long time thread, listens for incoming connections and handles them.
/// Delivers trunk to the clients connected to it. (that should be other agents)
/// Connects to other agents and requests trunk from them.  
/// </summary>
public class Agent
{
    public Agent()
    {
        this.T = new Trunk();
    }

    public Trunk T
    {
        get; private set;
    }
}
