using Microsoft.Extensions.DependencyInjection;

public class TP3Client
{
    public TP3Client()
    {
        var t = new TP3.Protocol.Client.TP3Client(1234, new Microsoft.Extensions.Logging.Abstractions.NullLogger<TP3.Protocol.Client.TP3Client>(), 5);
    }
}

public static class IoC
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<TP3Client>();
    }        
}