using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Logic.Templates;

/// <summary>
/// Service used to manage namespaces in tp3, create template, and manage. Should be used by the agent to manage namespaces and services.
/// </summary>
internal class NameSpaces : BaseDirectoryNode, IService
{
    public NameSpaces() : base()
    {
        base.AddChild(new Templates());
    }

    public void Dispose()
    {
    }

    public async Task Init(IAgent me)
    {
    }

    public async Task Start()
    {
    }

    public async Task Stop()
    {
    }
}
