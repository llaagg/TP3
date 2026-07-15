using TP3.Interfaces;

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
            new Screen()
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
