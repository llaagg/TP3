using TP3.Interfaces;

namespace TP3.Service.FileSystem;

public class StateNode : INode
{
    public StateNode()
    {
        this.Name = "state";
    }

    public string Name { get; set; }


    public IEnumerable<INode>? Children
    {
        get
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                yield return new FileSystemNode(drive.Name);
            }
        }
    }

    public Stream? Data
    {
        get
        {
            return null!;
        }
    }
}