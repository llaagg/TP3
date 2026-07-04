using TP3.GUI.Common.Controls;

namespace TP3.GUI;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		var c = new Connection();

		this.Content = c;
	}
}
