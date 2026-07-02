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

    public Task<ITP3DataStream> Open(INode node)
    {
        var fsnode = node as FileSystemNode;
        return Task.FromResult<ITP3DataStream>(new FileStreanReader(new FileStream(fsnode!.AbsolutePath, FileMode.Open, FileAccess.Read)));
    }

}

public class FileStreanReader : ITP3DataStream
{
    /// <summary>
    /// Prefered unit for reading the data
    /// </summary>
    public uint Iounit { get; } = 0;
    private readonly FileStream fileStream;

    public FileStreanReader(FileStream fileStream)
    {
        this.fileStream = fileStream;
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count)
    {
        return await fileStream.ReadAsync(buffer, offset, count);
    }

    public void Dispose()
    {
        fileStream.Dispose();
    }
}


