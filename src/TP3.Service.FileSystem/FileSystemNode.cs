using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.FileSystem;

public class FileSystemNode : INode
{
    public FileSystemNode(string absolutePath, string qid)
    {
        this.AbsolutePath = absolutePath;
        this.Qid = qid;
        this.IsDirectory = Directory.Exists(absolutePath);
        this.Name = Path.GetFileName(absolutePath);
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            this.Name = absolutePath;
        }
    }

    public string Qid { get; }

    public string Name { get; set; }

    public NodeType NodeType => IsDirectory ? NodeType.Directory : NodeType.File;

    public string AbsolutePath { get; private set; }

    public bool IsDirectory { get; private set; }

    public IEnumerable<INode>? Children
    {
        get
        {
            if (!IsDirectory)
            {
                yield break;
            }

            var directories = Directory.GetDirectories(this.AbsolutePath);
            foreach (var directory in directories)
            {
                yield return new FileSystemNode(directory, $"fs:{Guid.NewGuid():N}");
            }

            var files = Directory.GetFiles(this.AbsolutePath);
            foreach (var file in files)
            {
                yield return new FileSystemNode(file, $"fs:{Guid.NewGuid():N}");
            }
        }
    }

}
