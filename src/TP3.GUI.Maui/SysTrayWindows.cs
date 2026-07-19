#if WINDOWS
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using TP3.GUI.Maui;
#endif
using Microsoft.Maui.LifecycleEvents;

public static class SysTrayWindows
{
    public static MauiAppBuilder UseSysTray(this MauiAppBuilder builder)
    {
        builder.UseNotifyIcon();
        return builder;
    }

    public static MauiAppBuilder AddSysTray(this MauiAppBuilder builder)
    {
		// no windows no systray
		// windows will override this with a real implementation
        builder.Services.AddSingleton<ITrayWindowService, NullTrayWindowService>();

        #if WINDOWS
		builder.Services.AddSingleton<WindowsTrayService>();
		builder.Services.AddSingleton<ITrayWindowService>(serviceProvider => serviceProvider.GetRequiredService<WindowsTrayService>());
		builder.ConfigureLifecycleEvents(events =>
		{
			events.AddWindows(windows =>
			{
				windows.OnWindowCreated(window =>
				{
					var trayService = IPlatformApplication.Current?.Services?.GetService<WindowsTrayService>();
					trayService?.Initialize(window);
				});
			});
		});

        #endif

        return builder;
    }
}