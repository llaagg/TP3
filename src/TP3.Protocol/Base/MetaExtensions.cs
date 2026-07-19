using TP3.Interfaces;

namespace TP3.Protocol.Base;

public enum MetaField
{
    Desciption,
    UTFSymbol,
    
}

public static class MetaExtensions
{
    public static INode Meta(this INode node, MetaField field, string value, string langue = "en")
    {
        var metadata = node.GetMeta();
        if(metadata == null)
        {
            // if this is canooncal node, we need to create a new meta node and add it to the parent
            if(node is )
        }


        if (node. is IMeta metaNode)
        {
            metaNode.Properties.Add(new MetaProperty(field.ToString(), value, langue));
            return metaNode;
        }
        return node;
    }
}