using Microsoft.Extensions.Logging;


namespace TP3.GUI.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{

		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var bootFilePath = Path.Combine(localAppData, "TP3", "etc", "boot.tp3");

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSysTray()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			})
			.AddSysTray();

		builder.Services.AddLogging();
		builder.Services.AddMauiBlazorWebView();

		builder.Services.RegisterTP3();
        builder.Services.AddSingleton<IServerManager, ServerManager>();
		builder.Services.AddSingleton<IService, TP3.Service.FileSystem.FileSystemService>();
		builder.Services.AddSingleton<IService, TP3.Service.Remote.RemotesService>();
		builder.Services.AddSingleton<IService>(_ => new TP3.Service.Attached.AttachedService(
			port: TP3Consts.DefaultServerPort,
			logger: _.GetRequiredService<ILogger>()
		));
		builder.Services.AddSingleton<IService>(_ => new TP3.Service.WebDav.WebDavService(
			prefix: "http://localhost:19080/",
			autoMountWindows: true,
			driveLetter: "T:"));
		builder.Services.AddSingleton<IService>(serviceProvider =>
			{
				return new TP3.Service.IPC.IpcService(
					port: TP3Consts.DefaultIPCPort,
					serviceProvider.GetRequiredService<ILogger>());
			});
		builder.Services.AddSingleton<IService, TP3.Service.PS.PersonalSystem>();
		builder.Services.AddSingleton<IService, InitService>();

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
