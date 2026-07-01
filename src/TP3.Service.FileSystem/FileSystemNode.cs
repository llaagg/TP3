using TP3.Interfaces;

namespace TP3.Service.FileSystem;

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

    public ITP3Stream? Data
    {
        get
        {
            if (this.IsDirectory)
                return null!;
            else
            {
                throw new NotImplementedException("File data streaming is not implemented yet.");
            }
        }
    }
}
