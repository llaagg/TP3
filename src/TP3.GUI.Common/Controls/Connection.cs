namespace TP3.GUI.Common.Controls;

public class Connection : ContentView
{
    public Connection()
    {
        var PanelContent = new VerticalStackLayout
        {
            Spacing = 10
        };

        // 2. Create default panel elements
        Label panelHeader = new Label
        {
            Text = "Connection Panel",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkSlateBlue
        };

        // Add the default header to our content layout
        PanelContent.Children.Add(panelHeader);

        this.Content = PanelContent;
    }
}