namespace TP3.Interfaces;

public interface IAgent
{    
    INode T { get; }

    MetaData MetaData { get; }

    string HandleRequest(string request);
}