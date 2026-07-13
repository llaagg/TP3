using Microsoft.Extensions.Logging;

namespace TP3.GUI.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});
		builder.Services.AddLogging(logging =>
		{
			
		});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.RegisterTP3();
        builder.Services.AddSingleton<IServerManager, ServerManager>();

		builder.Services.AddSingleton<IService, TP3.Service.FileSystem.FileSystemService>();
		builder.Services.AddSingleton<IService, TP3.Service.Remote.RemotesService>();
		builder.Services.AddSingleton<IService, TP3.Service.CloudFilter.FoldersService>();
		builder.Services.AddSingleton<IService, TP3.Service.Shell.ShellService>();
		
		builder.Services.AddSingleton<IService>(serviceProvider =>
		{
			return new TP3.Service.IPC.IpcService(5001, serviceProvider.GetRequiredService<ILogger<TP3.Service.IPC.IpcService>>());
		});

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		
		var serverManager = app.Services.GetRequiredService<IServerManager>();
		serverManager.StartServer();

		return app;
	}
}
