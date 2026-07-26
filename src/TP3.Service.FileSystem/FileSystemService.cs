using System.Text;
using TP3.Interfaces;
using TP3.Protocol;
using TP3.Protocol.Base;

namespace TP3.Service.FileSystem;

/// <summary>
/// Service that allows access to filesystem, in tp3 space
/// </summary>
public class FileSystemService : BaseDirectoryNode, IService
{
    public FileSystemService()
        : base("filesystem")
    {
        this.State = 
            new StateNode("state")
                .Meta(MetaField.Description, 
@"This node provides access to the filesystem of the host machine. 

It allows you to navigate through directories and access files. Uses access to files, and provides acces to files and folders on disk.")
                .Meta(MetaField.UTFSymbol, "🖴");

        this.Control = new BaseDirectoryNode("control");

        this.Meta(MetaField.Description,
@"This node provides access to the filesystem of the host machine.")
            .Meta(MetaField.UTFSymbol, "🖴");

        this.AddMetaNodes(this.Control);
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

    override public IEnumerable<INode>? Children => new INode[] { State, Control };
}
