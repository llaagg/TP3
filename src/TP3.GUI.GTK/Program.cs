using Gtk;


// start agent

AgentInstance.Agent = TP3.Agent.Logic.Agent.Main(args);

Application.Init();

var window = new TP3.GUI.GTK.MainWindow();
window.ShowAll();

Application.Run();
