using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Service.PS;

public class PersonalSystem : BaseDirectoryNode, IService
{
    public PersonalSystem() : base()
    {
        base.AddChild(state);
    }

    INode state = new BaseDirectoryNode("state", null,
        new List<INode>
        {
            new Displays(),
            new AutoStart(),
        }
    );

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
