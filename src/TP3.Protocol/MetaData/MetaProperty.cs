using TP3.Interfaces;

namespace TP3.Protocol.Base;

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