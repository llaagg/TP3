using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol.Base;

namespace TP3.Service.FileSystem;

public class StateNode : BaseNodeWithMeta
{
    public StateNode(string qid)
    {
        this.Id = qid;
        this.Name = "state";
    }

    public override string Id { get; }

    public override string Name { get; }

    public override NodeType NodeType => NodeType.Directory;

    public override IEnumerable<INode>? Children
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

    public override ulong Length => 0;

    public override Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
}
