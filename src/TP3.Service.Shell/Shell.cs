using TP3.Interfaces;
using TP3.Protocol;
namespace TP3.Service.Shell;

public class Shell : BaseDirectoryNode
{
    public Shell(string name) : 
        base(name)
    {
        this.inStream = new MemoryStream();
        this.outStream = new MemoryStream();

        // two streams in - and out
        this.In = new StreamNode(() => inStream);
        this.Out = new StreamNode(() => outStream);

        this.childs = new List<INode>() { In, Out };
    }

    private Stream inStream;
    private Stream outStream;

    public StreamNode In { get; }
    public StreamNode Out { get; }

    private List<INode> childs;

    override public IEnumerable<INode>? Children => this.childs;
}