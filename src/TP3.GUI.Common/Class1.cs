using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace MauiAppPanelExample
{
    public class MyPanelControl : ContentView
    {
        // Expose a public layout container so pages can add elements directly to this panel
        public VerticalStackLayout PanelContent { get; private set; }

        public MyPanelControl()
        {
            // 1. Create the internal content container
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

            // 3. Wrap everything in a styled Border container
            Border borderWrapper = new Border
            {
                Stroke = Colors.LightGray,
                StrokeThickness = 1.5,
                Background = Colors.White,
                Padding = new Thickness(15),
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(8)
                },
                Content = PanelContent // Put the layout inside the border
            };

            // 4. Set the Border as the main visual root of this custom control
            this.Content = borderWrapper;
        }
    }
}
