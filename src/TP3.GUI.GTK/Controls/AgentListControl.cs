using Gtk;
using Gdk;
using System.Collections.Generic;

namespace TP3.GUI.GTK.Controls;

public class AgentListControl : Box
{
    private readonly ListBox listBox;

    public AgentListControl()
        : base(Orientation.Vertical, 8)
    {
        var title = new Label("Connections")
        {
            Halign = Align.Start,
            MarginTop = 6,
            MarginBottom = 6
        };

        PackStart(title, false, false, 0);

        listBox = new ListBox
        {
            SelectionMode = SelectionMode.None,
            Hexpand = true,
            Vexpand = true
        };

        var scrolledWindow = new ScrolledWindow
        {
            ShadowType = ShadowType.EtchedIn,
            HscrollbarPolicy = PolicyType.Automatic,
            VscrollbarPolicy = PolicyType.Automatic,
            Hexpand = true,
            Vexpand = true
        };
        scrolledWindow.Add(listBox);

        PackStart(scrolledWindow, true, true, 0);

        PopulateAgents(new[]
        {
            new ConnectionItem("Computer", "Desktop connection", "computer", "💻"),
            new ConnectionItem("Mobile", "Phone connection", "smartphone", "📱"),
            new ConnectionItem("Router", "Linux router", "network-wired", "📡"),
            new ConnectionItem("OneDrive", "Cloud storage", "folder-cloud", "☁️")
        });
    }

    public void PopulateAgents(IEnumerable<ConnectionItem> agents)
    {
        foreach (var child in listBox.Children)
        {
            listBox.Remove(child);
        }

        foreach (var agent in agents)
        {
            var row = new ListBoxRow();
            var rowBox = new Box(Orientation.Horizontal, 8)
            {
                Margin = 6,
                Hexpand = true,
                Vexpand = false
            };

            var icon = CreateIcon(agent.IconName, agent.IconFallback);
            var labels = new Box(Orientation.Vertical, 2)
            {
                Hexpand = true,
                Vexpand = true
            };

            var nameLabel = new Label(agent.Name)
            {
                Xalign = 0,
                Halign = Align.Start
            };
            var descriptionLabel = new Label(agent.Description)
            {
                Xalign = 0,
                Halign = Align.Start,
                Name = "smallLabel"
            };

            labels.PackStart(nameLabel, false, false, 0);
            labels.PackStart(descriptionLabel, false, false, 0);

            rowBox.PackStart(icon, false, false, 0);
            rowBox.PackStart(labels, true, true, 0);
            row.Add(rowBox);
            listBox.Add(row);
        }

        listBox.ShowAll();
    }

    private static Widget CreateIcon(string iconName, string fallback)
    {
        try
        {
            var theme = IconTheme.Default;
            if (theme is not null)
            {
                var pixbuf = theme.LoadIcon(iconName, 28, IconLookupFlags.UseBuiltin);
                if (pixbuf is not null)
                {
                    return new Image(pixbuf) { Yalign = 0.5f };
                }
            }
        }
        catch
        {
            // Fallback to emoji label when icon name cannot be loaded.
        }

        return new Label(fallback)
        {
            Yalign = 0.5f,
            Halign = Align.Start
        };
    }
}

public sealed class ConnectionItem
{
    public string Name { get; }
    public string Description { get; }
    public string IconName { get; }
    public string IconFallback { get; }

    public ConnectionItem(string name, string description, string iconName, string iconFallback)
    {
        Name = name;
        Description = description;
        IconName = iconName;
        IconFallback = iconFallback;
    }
}
