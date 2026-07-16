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
            new Displays(),
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

internal class Displays : BaseDirectoryNode, INode
{
    public Displays() : base("displays")
    {
    }

    public override IEnumerable<INode>? Children
    {
        get
        {
            var screensList = WindowsScreenCapture.GetScreens();
            foreach (var screen in screensList)
            {
                yield return new Screen(screen.Handle, screen.IsPrimary, screen.Width, screen.Height);
            }
        }
    }
}