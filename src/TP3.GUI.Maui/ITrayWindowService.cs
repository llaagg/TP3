namespace TP3.GUI.Maui;

internal interface ITrayWindowService
{
	void ToggleWindow();

	void ShowWindow();

	void HideWindow();

	void ExitApplication();
}

internal sealed class NullTrayWindowService : ITrayWindowService
{
	public static readonly NullTrayWindowService Instance = new();

	public NullTrayWindowService()
	{
		
	}

	public void ToggleWindow()
	{
	}

	public void ShowWindow()
	{
	}

	public void HideWindow()
	{
	}

	public void ExitApplication()
	{
		Application.Current?.Quit();
	}
}