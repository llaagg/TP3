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

    public ITP3Stream? Data => null;

    public IEnumerable<INode>? Children
    {
        get
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                var normalized = drive.Name.TrimEnd(Path.DirectorySeparatorChar);
                var qid = $"fs:{normalized}:{Guid.NewGuid():N}";
                yield return new FileSystemNode(drive.Name, qid);
            }
        }
    }
}
