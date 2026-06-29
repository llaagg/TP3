using Gtk;
using Microsoft.Extensions.Logging;
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

        var toolbar = new Box(Orientation.Horizontal, 8)
        {
            Hexpand = true
        };

        var addLocalButton = new Button("Add Local")
        {
            Halign = Align.Start
        };

        var nodeInfo = new NodeInfoControl();

        addLocalButton.Clicked += (_, _) =>
        {
            var result = AddLocalConnection();
            nodeInfo.Text = result;
        };

        toolbar.PackStart(addLocalButton, false, false, 0);

        var contentBox = new Box(Orientation.Horizontal, 12)
        {
            Hexpand = true,
            Vexpand = true
        };

        var agentList = new AgentListControl();

        contentBox.PackStart(agentList, true, true, 0);
        contentBox.PackStart(nodeInfo, true, true, 0);

        mainBox.PackStart(toolbar, false, false, 0);
        mainBox.PackStart(contentBox, true, true, 0);

        Add(mainBox);
    }

    private string AddLocalConnection()
    {
        if (AgentInstance.Host is null)
        {
            return "Agent host is not initialized.";
        }

        try
        {
            var namespaceName = $"local-{Guid.NewGuid():N}";
            var ns = AgentInstance.Host.CreateNamespace(namespaceName);
            return $"Created local namespace: {ns.Name}";
        }
        catch (Exception ex)
        {
            return $"Add local failed: {ex.Message}";
        }
    }
}
