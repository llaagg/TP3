using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Service.LLM;

public class LLMService : BaseDirectoryNode, IService
{
    public LLMService() : base("llm-service")
    {
        this.State = new BaseDirectoryNode("state");
        this.Control = new BaseDirectoryNode("control");

        this.AddChild(this.State);
        this.AddChild(this.Control);
    }

    public INode State { get; }

    public INode Control { get; }

    public INode? Events => null;

    public Task Init(IAgent me)
    {
        return Task.CompletedTask;
    }

    public Task Start()
    {
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
