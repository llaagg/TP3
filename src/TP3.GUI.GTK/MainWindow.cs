using Gtk;
using TP3.GUI.GTK.Controls;

namespace TP3.GUI.GTK;

public class MainWindow : Window
{
    public MainWindow()
        : base("TP3 Agent")
    {
        DefaultWidth = 900;
        DefaultHeight = 520;
        SetPosition(WindowPosition.Center);
        DeleteEvent += (_, _) => Application.Quit();

        var mainBox = new Box(Orientation.Vertical, 12)
        {
            Margin = 12,
            Hexpand = true,
            Vexpand = true
        };

        var contentBox = new Box(Orientation.Horizontal, 12)
        {
            Hexpand = true,
            Vexpand = true
        };

        var agentList = new AgentListControl();
        var nodeInfo = new NodeInfoControl();

        contentBox.PackStart(agentList, true, true, 0);
        contentBox.PackStart(nodeInfo, true, true, 0);

        mainBox.PackStart(contentBox, true, true, 0);

        Add(mainBox);
    }
}
