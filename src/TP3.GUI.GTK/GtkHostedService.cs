using Revuo.Chat.Client.Gtk;
using Microsoft.Extensions.Hosting;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using System.Threading;
using System;

[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
internal class GtkHostedService : IHostedService
{
	

	public GtkHostedService(IHostApplicationLifetime lifetime, IServiceProvider serviceProvider)
	{
		WebKit.Module.Initialize();

		_serviceProvider = serviceProvider;
		_app = Adw.Application.New("org.gir.core", Gio.ApplicationFlags.FlagsNone);

		_app.OnActivate += (sender, args) =>
		{
			var window = Gtk.ApplicationWindow.New((Adw.Application)sender);
			window.Title = "Revuo";
			window.SetDefaultSize(800, 600);

			var webView = new BlazorWebView(_serviceProvider);
			window.SetChild(webView);
			window.Show();

			// Allow opening developer tools
			webView.GetSettings().EnableDeveloperExtras = true;
		};

		_app.OnShutdown += (sender, args) =>
		{
			lifetime.StopApplication();
		};

		lifetime.ApplicationStarted.Register(() => 
		{
			Task.Run(() => 
			{
				Environment.ExitCode = _app.Run(0, []);
			});
		});

		lifetime.ApplicationStopping.Register(() =>
		{
			_app.Quit();
		});
	}

	readonly IServiceProvider _serviceProvider;
	readonly Adw.Application _app;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}