using TP3.Interfaces;
namespace TP3.Service.Shell;

public class State: BaseDirectoryNode
{
    private List<INode> childs;
    private ShellService shellService;

    public State(string name) : base(name)
    {
        this.childs = new List<INode>();
        this.shellService = null;
    }

    public void Init(IAgent me, ShellService shellService)
    {
        this.shellService = shellService;
    }

    internal void AddChild(Shell terminalNode)
    {
        childs.Add(terminalNode);
    }

    override public IEnumerable<INode>? Children => this.childs;
}