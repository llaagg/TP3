using Revuo.Chat.Client.Gtk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using TP3.GUI.GTK;

class GtkProgram
{
	
	private static async Task Main(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);
	
        builder.Services.AddLocalization();
		builder.Services.AddSingleton(new BlazorWebViewOptions
		{
			RootComponent = typeof(App),
			HostPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html")
		});
		builder.Services.AddHostedService<GtkHostedService>();

		using var application = builder.Build();
		
		var logger = application.Services.GetRequiredService<ILogger<App>>();
		logger.LogInformation($"Starting");
        
		await application.RunAsync();
	}
}
