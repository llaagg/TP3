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

    public string SetStartingFolder { get; private set; }

    public INode State { get; private set; } = null!;

    public INode Control { get; private set; } = null!;

    public INode Events { get; private set; } = null!;

    public async Task Init(IAgent me)
    {
        this.SetStartingFolder = "C:\\";
        this.State = new FileSystemNode(this.SetStartingFolder);
    }
}

public class FileSystemNode : INode
{
    public FileSystemNode(string absolutePath)
    {
        this.AbsolutePath = absolutePath;
        this.IsDirectory = Directory.Exists(absolutePath);
        this.Name = Path.GetFileName(absolutePath);
    }

    public string Name { get; set; }

    public string AbsolutePath { get; private set; }

    public bool IsDirectory { get; private set; }

    public IEnumerable<INode>? Children
    {
        get
        {
            if (IsDirectory)
            {
                var directories = Directory.GetDirectories(this.AbsolutePath);
                foreach (var directory in directories)
                {
                    yield return new FileSystemNode(directory);
                }

                var files = Directory.GetFiles(this.AbsolutePath);
                foreach (var file in files)
                {
                    yield return new FileSystemNode(file);
                }
            }
            else
            {
                yield return null!;
            }
        }
    }

    public Stream? Data
    {
        get
        {
            if (this.IsDirectory)
                return null!;
            else
            {
                return File.OpenRead(this.AbsolutePath);
            }
        }
    }
}

