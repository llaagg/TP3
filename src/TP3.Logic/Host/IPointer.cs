using TP3.Interfaces;

namespace TP3.Agent.Logic.Transport;

public class Pointer : IPointer
{
    public INode Node { get; set; }
    public ITP3DataStream? Data { get; set; }
}