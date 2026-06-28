using Gtk;

Application.Init();

var window = new Window("TP3 Agent")
{
	DefaultWidth = 420,
	DefaultHeight = 220
};

window.SetPosition(WindowPosition.Center);
window.DeleteEvent += (_, _) => Application.Quit();

var label = new Label("Hello from GtkSharp");
window.Add(label);

window.ShowAll();
Application.Run();
