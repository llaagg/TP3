using TP3.GUI.Common.Controls;

namespace TP3.GUI;

public partial class MainPage : ContentPage
{
	int count = 0;

	public VerticalStackLayout PanelContent { get; private set; }

	public MainPage()
	{
		var c = new Connection();

		this.Content = c;
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		// count++;

		// if (count == 1)
		// 	CounterBtn.Text = $"Clicked {count} time";
		// else
		// 	CounterBtn.Text = $"Clicked {count} times";

		// SemanticScreenReader.Announce(CounterBtn.Text);
	}
}
