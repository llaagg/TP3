using Gtk;

namespace TP3.GUI.GTK.Controls;

public class NodeInfoControl : Box
{
    private readonly TextView textView;

    public NodeInfoControl()
        : base(Orientation.Vertical, 8)
    {
        var title = new Label("Node Info")
        {
            Halign = Align.Start,
            MarginTop = 6,
            MarginBottom = 6
        };

        PackStart(title, false, false, 0);

        textView = new TextView
        {
            WrapMode = WrapMode.WordChar,
            Editable = true,
            CursorVisible = true,
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
        scrolledWindow.Add(textView);

        PackStart(scrolledWindow, true, true, 0);
    }

    public string Text
    {
        get => textView.Buffer.Text;
        set => textView.Buffer.Text = value;
    }
}
