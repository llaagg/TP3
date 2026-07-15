using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.FileSystem;

public class StateNode : INode
{
    public StateNode(string qid)
    {
        this.Id = qid;
        this.Name = "state";
    }

    public string Id { get; }

    public string Name { get; set; }

    public NodeType NodeType => NodeType.Directory;

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

    public ulong Length => 0;

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
}
