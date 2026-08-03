using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Service.PS;

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