using Microsoft.Maui.Controls;

namespace TP3.Maui.Client;

public class App : Application
{
    public App(MainPage mainPage)
    {
        MainPage = new NavigationPage(mainPage);
    }
}
