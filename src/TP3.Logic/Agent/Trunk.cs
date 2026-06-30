namespace TP3.Agent.Logic.Agent;

/// <summary>
/// this is the place where all streams will be availble
/// covering:
///  storage 
///  events (incl. events that allow to start compute) like bus of events
///  stream (incl. multimedia streams)
/// each stream should provide metadata, 
///  r or w what is there, where is it, what is the transport
///  is 
///  
/// </summary>
public class Trunk : INode
{
    Dictionary<string, Func<TP3Stram>> streams = new Dictionary<string, Func<TP3Stram>>()
    {
        { "Storage", Storage },
    };

    public string Id => throw new NotImplementedException();

    public string Value => throw new NotImplementedException();

    public IEnumerable<INode> Leafs => throw new NotImplementedException();

    private static TP3Stram Storage()
    {
        return new TP3Stram();
    }

    public List<string> Streams()
    {
        return new List<string>
        {
            "Storage",
            "Bus",
            "Streams"
        };
    }

    public TP3Stram Get(string name)
    {
        if (streams.TryGetValue(name, out var streamFunc))
        {
            return streamFunc();
        }
        throw new KeyNotFoundException($"Stream '{name}' not found.");
    }
}
