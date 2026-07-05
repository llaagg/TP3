using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Web;
using WebKit;
using MemoryInputStream = Gio.Internal.MemoryInputStream;
using Uri = System.Uri;

namespace Revuo.Chat.Client.Gtk;

[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
public class BlazorWebView : WebView
{
	public BlazorWebView(IServiceProvider serviceProvider)
	{
		_ = new WebViewManager(this, serviceProvider);
	}
}

[UnsupportedOSPlatform("OSX")]
[UnsupportedOSPlatform("Windows")]
class WebViewManager : Microsoft.AspNetCore.Components.WebView.WebViewManager
{
	const string Scheme = "app";
	static readonly Uri BaseUri = new($"{Scheme}://localhost/");

	public WebViewManager(WebView webView, IServiceProvider serviceProvider) : base(
		serviceProvider,
		Dispatcher.CreateDefault(),
		BaseUri,
		new PhysicalFileProvider(serviceProvider.GetRequiredService<BlazorWebViewOptions>().ContentRoot),
		new(),
		serviceProvider.GetRequiredService<BlazorWebViewOptions>().RelativeHostPath)
	{
		var options = serviceProvider.GetRequiredService<BlazorWebViewOptions>();
		_relativeHostPath = options.RelativeHostPath;
		var rootComponent = options.RootComponent;
		_logger = serviceProvider.GetService<ILogger<WebViewManager>>();

		_webView = webView;

		// This is necessary to automatically serve the files in the `_framework` virtual folder.
		// Using `file://` will cause the webview to look for the `_framework` files on the file system,
		// and it won't find them.
		if (_webView.WebContext is null)
		{
			throw new Exception("WebView.WebContext is null");
		}
		_webView.WebContext.RegisterUriScheme(Scheme, HandleUriScheme);

		Dispatcher.InvokeAsync(async () =>
		{
			await AddRootComponentAsync(rootComponent, "#app", ParameterView.Empty);
		});

		var ucm = webView.GetUserContentManager();
		ucm.AddScript(UserScript.New(
			source:
			"""
				window.__receiveMessageCallbacks = [];

				window.__dispatchMessageCallback = function(message) {
				    window.__receiveMessageCallbacks.forEach(function(callback) { callback(message); });
				};

				window.external = {
				    sendMessage: function(message) {
				        window.webkit.messageHandlers.webview.postMessage(message);
				    },
				    receiveMessage: function(callback) {
				        window.__receiveMessageCallbacks.push(callback);
				    }
				};
			""",
			injectedFrames: UserContentInjectedFrames.AllFrames,
			injectionTime: UserScriptInjectionTime.Start,
			null,
			null)
		);

		UserContentManager.ScriptMessageReceivedSignal.Connect(ucm, (_, signalArgs) =>
		{
			var result = signalArgs.Value;
			MessageReceived(BaseUri, result.ToString());
		}, true, "webview");

		if (!ucm.RegisterScriptMessageHandler("webview", null))
		{
			throw new Exception("Could not register script message handler");
		}

		Navigate("/");
	}

	readonly WebView _webView;
	readonly string _relativeHostPath;
	readonly ILogger<WebViewManager>? _logger;

	void HandleUriScheme(URISchemeRequest request)
	{
		if (request.GetScheme() != Scheme)
		{
			throw new Exception($"Invalid scheme \"{request.GetScheme()}\"");
		}

		var uri = request.GetUri();
		var path = request.GetPath();
		if (path == "/" || !Path.HasExtension(path))
		{
			uri = new Uri(BaseUri, _relativeHostPath).ToString();
		}

		_logger?.LogDebug($"Fetching \"{uri}\"");

		if (TryGetResponseContent(uri, false, out var statusCode, out var statusMessage, out var content, out var headers))
		{
			using var ms = new MemoryStream();
			content.CopyTo(ms);
			var streamPtr = MemoryInputStream.NewFromData(ref ms.GetBuffer()[0], (nint)ms.Length, _ => { });
			var inputStream = new Gio.InputStream(new Gio.Internal.InputStreamHandle(streamPtr, false));
			request.Finish(inputStream, ms.Length, headers["Content-Type"]);
		}
		else
		{
			_logger?.LogError($"Failed to serve \"{uri}\". {statusCode} - {statusMessage}");

			//throw new Exception($"Failed to serve \"{uri}\". {statusCode} - {statusMessage}");
		}
	}

	protected override void NavigateCore(Uri absoluteUri)
	{
		var targetUri = absoluteUri;
		if (!string.Equals(absoluteUri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
		{
			var pathAndQuery = absoluteUri.IsAbsoluteUri
				? absoluteUri.PathAndQuery.TrimStart('/')
				: absoluteUri.OriginalString.TrimStart('/');
			targetUri = new Uri(BaseUri, pathAndQuery);
		}

		_logger?.LogDebug($"Navigating to \"{absoluteUri}\" (normalized to \"{targetUri}\")");
		_webView.LoadUri(targetUri.ToString());
	}

	protected override async void SendMessage(string message)
	{
		var script = $"__dispatchMessageCallback(\"{HttpUtility.JavaScriptStringEncode(message)}\")";
		_logger?.LogDebug($"Dispatching `{script}`");
		_ = await _webView.EvaluateJavascriptAsync(script);
	}
}