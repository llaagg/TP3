using TP3.Interfaces;
using TP3.Protocol;

namespace TP3.Protocol;

public class MetaData : BaseDirectoryNode
{
    public MetaData()
        : base()
    {
        this.AddChild(new MetaDataProperty("name", "TP3 Service"));
    }
}

internal class MetaDataProperty : StreamNode
{
    public MetaDataProperty(string name, string value)
        : base(() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value)), name)
    {
        this.value = value;
    }

    private readonly string value;
}