using Gtk;
using System.Collections.Generic;

namespace TP3.GUI.GTK.Controls;

public class AgentListControl : Box
{
    private readonly ListStore listStore;

    public AgentListControl()
        : base(Orientation.Vertical, 8)
    {
        var title = new Label("Agents")
        {
            Halign = Align.Start,
            MarginTop = 6,
            MarginBottom = 6
        };

        PackStart(title, false, false, 0);

        listStore = new ListStore(typeof(string), typeof(string));

        var treeView = new TreeView(listStore)
        {
            HeadersVisible = true,
            Hexpand = true,
            Vexpand = true
        };

        treeView.AppendColumn("Name", new CellRendererText(), "text", 0);
        treeView.AppendColumn("Description", new CellRendererText(), "text", 1);

        var scrolledWindow = new ScrolledWindow
        {
            ShadowType = ShadowType.EtchedIn,
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic,
            Hexpand = true,
            Vexpand = true
        };
        scrolledWindow.Add(treeView);

        PackStart(scrolledWindow, true, true, 0);

        PopulateAgents(new[]
        {
            new AgentItem(AgentInstance.Agent),
        });
    }

    public void PopulateAgents(IEnumerable<AgentItem> agents)
    {
        listStore.Clear();

        foreach (var agent in agents)
        {
            listStore.AppendValues(agent.Name, agent.Description);
        }
    }
}

public sealed class AgentItem
{
    public string Name { get; }
    public string Description { get; }

    public AgentItem(TP3.Agent.Logic.Agent agent)
    {
        Name = agent.T?.GetType().Name;
        Description = agent.T?.GetType().FullName;
    }
}
