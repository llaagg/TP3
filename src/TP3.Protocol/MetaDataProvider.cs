using TP3.Interfaces;

namespace TP3.Service.FileSystem;

public class MetaDataProvider : BaseDirectoryNode
{
    public MetaDataProvider(IService service)
    {
        this.service = service;

        this.ByPath = new MetaDataByPathNode();

        this.AddChild(this.ByPath);
    }

    private IService service;
    private MetaDataByPathNode ByPath;
}

public class MetaDataByPathNode : BaseDirectoryNode
{
    public MetaDataByPathNode(INode? node)
        : base("bypath")
    {
        //let's allow to go by this path
        //and render

        if(node.NodeType == Messages.NodeType.Directory)
        {
            foreach(var child in node.Children ?? new List<INode>())
            {
                this.AddChild(new MetaDataByPathNode(child));
            }
        }
    }
}