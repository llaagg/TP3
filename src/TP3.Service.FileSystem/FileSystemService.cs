using System.Text;
using TP3.Interfaces;

namespace TP3.Service.FileSystem;

/// <summary>
/// Service that allows access to filesystem, in tp3 space
/// </summary>
public class FileSystemService : BaseDirectoryNode, IService
{
    public FileSystemService()
        : base("FileSystemService")
    {
        this.State = new StateNode("state");
    }

    public INode State { get; private set; } = null!;

    #warning TODO: We need: Create, Delete
    public INode Control { get; private set; } = null!;

    public INode Events { get; private set; } = null!;

    public async Task Init(IAgent me)
    {
    }

    public async Task Start()
    {
    }

    public async Task Stop()
    {
    }

    public void Dispose()
    {
    }

    override public IEnumerable<INode>? Children => new INode[] { State };
}

