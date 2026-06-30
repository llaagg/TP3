using TP3.Interfaces;

namespace TP3.Service.FileSystem;

/// <summary>
/// Service that allows access to filesystem, in tp3 space
/// </summary>
public class FileSystemService : IService
{
    public FileSystemService()
    {
    }

    public object State => throw new NotImplementedException();

    public object Control => throw new NotImplementedException();

    public object Events => throw new NotImplementedException();

    public MetaData MetaData => throw new NotImplementedException();

    public string SetStartingFolder { get; private set; }

    public async Task Init(IAgent me)
    {
        this.SetStartingFolder = "C:\\";
    }
}
