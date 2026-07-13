using TP3.Interfaces;
using TP3.Protocol;
using System.IO.Pipelines;
namespace TP3.Service.Shell;

public class Shell : BaseDirectoryNode
{
    public Shell(string name) : 
        base(name)
    {
        this.inPipe = new Pipe();
        this.outPipe = new Pipe();

        // Client writes input here; service reads from inReadStream.
        this.inStream = this.inPipe.Writer.AsStream(leaveOpen: true);
        this.inReadStream = this.inPipe.Reader.AsStream(leaveOpen: true);

        // Service writes output here; client reads from outStream.
        this.outWriteStream = this.outPipe.Writer.AsStream(leaveOpen: true);
        this.outStream = this.outPipe.Reader.AsStream(leaveOpen: true);

        // two streams in - and out
        this.In = new StreamNode(() => inStream, "in");
        this.Out = new StreamNode(() => outStream, "out");

        this.childs = new List<INode>() { In, Out };       

    }

    private readonly Pipe inPipe;
    private readonly Pipe outPipe;

    public Stream inStream;
    public Stream outStream;

    private readonly Stream inReadStream;
    private readonly Stream outWriteStream;

    public Stream GetInReadStream() => this.inReadStream;
    public Stream GetOutWriteStream() => this.outWriteStream;

    public StreamNode In { get; }
    public StreamNode Out { get; }

    private List<INode> childs;

    override public IEnumerable<INode>? Children => this.childs;
}