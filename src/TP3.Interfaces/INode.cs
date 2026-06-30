namespace TP3.Interfaces;

public interface IAgent
{    
    INode T { get; }
    Task AddService(IService service);
    string HandleRequest(string request);
}