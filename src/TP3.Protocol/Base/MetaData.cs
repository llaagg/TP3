using TP3.Interfaces;

namespace TP3.Protocol.Base;

public class MetaData : IMeta
{
    List<IMetaProperty> _properties = new List<IMetaProperty>();
    public List<IMetaProperty> Properties => _properties;
}
