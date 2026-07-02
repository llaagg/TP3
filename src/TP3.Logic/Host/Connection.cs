using TP3.Interfaces;

namespace TP3.Agent.Logic.Transport;

public class Connection
{   
    public INetworkPipe Session { get; set; }
    
    /// <summary>
    /// tags to pointers
    /// </summary>
    public Dictionary<string, Pointer> Pointers { get; set; } = new Dictionary<string, Pointer>();    
}
