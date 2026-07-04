namespace TP3.GUI;

public partial class MainPage : ContentPage
{
	int count = 0;

	public VerticalStackLayout PanelContent { get; private set; }

	public MainPage()
	{
		//InitializeComponent();

		PanelContent = new VerticalStackLayout
		{
			Spacing = 10
		};

		// 2. Create default panel elements
		Label panelHeader = new Label
		{
			Text = "Custom Panel Control",
			FontSize = 18,
			FontAttributes = FontAttributes.Bold,
			TextColor = Colors.DarkSlateBlue
		};

		// Add the default header to our content layout
		PanelContent.Children.Add(panelHeader);

		this.Content = PanelContent;
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
