using TP3.Interfaces;

namespace TP3.Protocol;

public class MetaDataProvider : BaseDirectoryNode
{
    public MetaDataProvider(IService service)
        : base("meta")
    {
        this.service = service;

        this.ByPath = new MetaDataByPathNode(service);

        this.AddChild(this.ByPath);
    }

    private IService service;
    private MetaDataByPathNode ByPath;
}

public class MetaDataByPathNode : BaseDirectoryNode
{
    private INode mapping;

    public MetaDataByPathNode(INode node)
        : base(node.Name)
    {
        mapping = node;

    }

    override public IEnumerable<INode>? Children
    {
        get
        {
            if(this.mapping.NodeType == Messages.NodeType.Directory)
            {
                return this.mapping.Children?.Select(child => new MetaDataByPathNode(child));
            }
            else
            {
                var md = this.mapping.GetMeta();
                return new List<INode>()
                {
                    new MetaDataProperty("name", this.mapping.Name),
                    new MetaDataProperty("type", this.mapping.NodeType.ToString()),
                };
            }
        }
    }
}