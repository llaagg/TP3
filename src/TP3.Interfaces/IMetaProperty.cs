namespace TP3.Interfaces;

public interface IMetaProperty
{
    public string Name { get; }

    /// <summary>
    /// MD format recommended
    /// </summary>
    public string GetValue(string langue = "en");
}
