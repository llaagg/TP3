#if WINDOWS
using H.NotifyIcon;
using MauiWindow = Microsoft.Maui.Controls.Window;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace TP3.GUI.Maui;

internal sealed class WindowsTrayService : ITrayWindowService, IDisposable
{
	private Microsoft.UI.Xaml.Window? nativeWindow;
	private AppWindow? appWindow;
	private bool isWindowVisible = true;
	private bool allowClose;

	public void Initialize(Microsoft.UI.Xaml.Window nativeWindow)
	{
		if (appWindow is not null)
		{
			return;
		}

		this.nativeWindow = nativeWindow;
		var hwnd = WindowNative.GetWindowHandle(nativeWindow);
		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
		appWindow = AppWindow.GetFromWindowId(windowId);
		appWindow.Closing += OnClosing;
	}

	private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
	{
		if (allowClose)
		{
			return;
		}

		args.Cancel = true;
		HideWindow();
	}

	public void ToggleWindow()
	{
		if (isWindowVisible)
		{
			HideWindow();
			return;
		}

		ShowWindow();
	}

	public void HideWindow()
	{
		var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
		window?.Dispatcher.Dispatch(() =>
		{
			window.Hide();
			isWindowVisible = false;
		});
	}

	public void ShowWindow()
	{
		var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
		window?.Dispatcher.Dispatch(() =>
		{
			window.Show();
			nativeWindow?.Activate();
			isWindowVisible = true;
		});
	}

	public void ExitApplication()
	{
		allowClose = true;
		var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
		window?.Dispatcher.Dispatch(() =>
		{
			appWindow?.Closing -= OnClosing;
			Microsoft.Maui.Controls.Application.Current?.Quit();
		});
	}

	public void Dispose()
	{
		if (appWindow is not null)
		{
			appWindow.Closing -= OnClosing;
		}
	}
}
#endif