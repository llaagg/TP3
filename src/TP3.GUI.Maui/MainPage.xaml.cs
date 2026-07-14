namespace TP3.GUI.Maui;

public partial class MainPage : ContentPage
{
	private readonly ITrayWindowService trayWindowService;

	public Command ToggleWindowCommand { get; }

	public Command ExitApplicationCommand { get; }

	public MainPage()
	{
		InitializeComponent();
		trayWindowService = IPlatformApplication.Current?.Services?.GetService<ITrayWindowService>() ?? NullTrayWindowService.Instance;
		ToggleWindowCommand = new Command(() => trayWindowService.ToggleWindow());
		ExitApplicationCommand = new Command(() => trayWindowService.ExitApplication());
		BindingContext = this;
		Loaded += OnLoaded;
	}

	private void OnLoaded(object? sender, EventArgs e)
	{
		TrayIcon.ForceCreate();
		Loaded -= OnLoaded;
	}
}
