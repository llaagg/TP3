using TP3.Interfaces;
using TP3.Protocol.MetaData;

namespace TP3.Protocol.Base;

public enum MetaField
{
    Description,
    UTFSymbol,

}

public static class MetaExtensions
{
    public static INode Meta(this INode node, MetaField field, string value, string langue = "en")
    {
        var metadata = node.GetMeta();

        MetaData? m = metadata as MetaData;
        if(m==null)
        {
            throw new InvalidOperationException(
                $"Cannot add meta to a node that does not support meta. "+
                $"(Meta type: {m.GetType().Name} expected: {nameof(MetaData)})");
        }


        m.Properties.Add(new MetaProperty(field.ToString())
        {
            Values = new Dictionary<string, string>()
            {
                {langue, value}
            }
        });
        return node;
        
    }

    public static List<INode> AddMetaNodes(this IService service, INode controlNode)
    {
        var list = new List<INode>()
        {
            new MetaLs(service)
                .Meta(MetaField.Description, "Lists all properties of metadata per convention")
                .Meta(MetaField.UTFSymbol, "📜")
                
            ,new Meta(service)
                .Meta(MetaField.Description, "Shows all properties and values of metadata as concatenated string")
                .Meta(MetaField.UTFSymbol, "📝")
            ,new MetaGet(service)
                .Meta(MetaField.Description, "Gets the value of a specific metadata property")
                .Meta(MetaField.UTFSymbol, "🔍")
                ,
        };

        if(controlNode is BaseDirectoryNode dirNode)
        {
            foreach(var n in list)
            {
                dirNode.AddChild(n);
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot add meta nodes to a node that does not support children. "+
                $"(Node type: {controlNode.GetType().Name} expected: {nameof(BaseDirectoryNode)})");
        }
        return list;
    }
}
