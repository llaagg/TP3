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
}

public class MetaProperty : IMetaProperty
{
    public MetaProperty(string name)
    {
        Name = name;    }

    public string Name { get; set; }

    public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

    public string GetValue(string langue = "en")
    {
        return Values.TryGetValue(langue, out var value) ? value : string.Empty;
    }
}