using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TP3.Interfaces;

namespace TP3.Maui.Client;

public class Program
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // fonts
            });

        // register TP3 services
        builder.Services.AddSingleton<AgentNode>();

        return builder.Build();
    }
}
