using TP3.Interfaces;

namespace TP3.Interfaces;

public interface IPointer
{
    INode Node { get; set; }

    ITP3DataStream? Data { get; set; }
}
