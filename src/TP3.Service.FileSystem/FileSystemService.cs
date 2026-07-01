using System.Text;
using TP3.Interfaces;

namespace TP3.Service.FileSystem;

/// <summary>
/// Service that allows access to filesystem, in tp3 space
/// </summary>
public class FileSystemService : IService
{
    public FileSystemService()
    {
        this.State = new StateNode("fs:state");
    }

    public INode State { get; private set; } = null!;

    public INode Control { get; private set; } = null!;

    public INode Events { get; private set; } = null!;

    public async Task Init(IAgent me)
    {
    }
}


