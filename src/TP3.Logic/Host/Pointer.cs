using TP3.Interfaces;

namespace TP3.Agent.Logic.Transport;

public class Pointer
{
    public INode Node { get; set; }

    public ITP3DataStream? Data { get; set; } = null!;
}
