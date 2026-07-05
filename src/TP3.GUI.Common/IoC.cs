using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class IoC
{
    public static void RegisterTP3(this IServiceCollection services)
    {
        services.AddSingleton<TP3ClientWrapper>();
        services.AddSingleton<ILogger, MauiLogger>();
    }        
}