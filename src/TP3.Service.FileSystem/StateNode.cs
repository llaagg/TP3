using TP3.Interfaces;

namespace TP3.Service.FileSystem;

public class StateNode : INode
{
    public StateNode(string qid)
    {
        this.Qid = qid;
        this.Name = "state";
    }

    public string Qid { get; }

    public string Name { get; set; }


    public IEnumerable<INode>? Children
    {
        get
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                yield return new FileSystemNode(drive.Name, $"fs:{Guid.NewGuid():N}");
            }
        }
    }

    public ITP3Stream? Data
    {
        get
        {
            return null!;
        }
    }
}