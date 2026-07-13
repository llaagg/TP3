using TP3.Interfaces;
using TP3.Messages;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace TP3.Service.WebDav;

public class WebDavService : BaseDirectoryNode, IService
{
    private const string DefaultPrefix = "http://localhost:19080/";
    private readonly HttpListener listener = new();
    private CancellationTokenSource? cts;
    private Task? acceptLoopTask;
    private INode? root;
    private readonly string prefix;
    private readonly bool autoMountWindows;
    private readonly string driveLetter;
    private bool mountedByService;

    public WebDavService(string? prefix = null, bool autoMountWindows = true, string driveLetter = "T:") : base("webdav")
    {
        this.prefix = NormalizePrefix(prefix ?? DefaultPrefix);
        this.autoMountWindows = autoMountWindows;
        this.driveLetter = NormalizeDriveLetter(driveLetter);
    }

    public void Dispose()
    {
        this.cts?.Cancel();
        if (this.listener.IsListening)
        {
            this.listener.Stop();
        }

        this.TryUnmountWindowsDrive();

        this.listener.Close();
        this.cts?.Dispose();
    }

    public Task Init(IAgent me)
    {
        this.root = me.T;

        if (!this.listener.Prefixes.Contains(this.prefix, StringComparer.OrdinalIgnoreCase))
        {
            this.listener.Prefixes.Add(this.prefix);
        }

        return Task.CompletedTask;
    }

    public Task Start()
    {
        if (this.root is null)
        {
            throw new InvalidOperationException("WebDavService was not initialized.");
        }

        if (this.listener.IsListening)
        {
            return Task.CompletedTask;
        }

        this.cts = new CancellationTokenSource();
        this.listener.Start();
        this.acceptLoopTask = Task.Run(() => this.AcceptLoop(this.cts.Token));

        this.TryMountWindowsDrive();
        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        if (!this.listener.IsListening)
        {
            return;
        }

        this.cts?.Cancel();
        this.listener.Stop();

        if (this.acceptLoopTask is not null)
        {
            await this.acceptLoopTask.ConfigureAwait(false);
        }

        this.acceptLoopTask = null;
        this.cts?.Dispose();
        this.cts = null;

        this.TryUnmountWindowsDrive();
    }

    private void TryMountWindowsDrive()
    {
        if (!OperatingSystem.IsWindows() || !this.autoMountWindows)
        {
            return;
        }

        if (this.mountedByService)
        {
            return;
        }

        if (!TryBuildWindowsUncPath(this.prefix, out var uncPath))
        {
            return;
        }

        // Best-effort reset of previous mapping.
        _ = RunProcess("net", $"use {this.driveLetter} /delete /y");

        var exitCode = RunProcess("net", $"use {this.driveLetter} \"{uncPath}\" /persistent:no");
        this.mountedByService = exitCode == 0;
    }

    private void TryUnmountWindowsDrive()
    {
        if (!OperatingSystem.IsWindows() || !this.autoMountWindows)
        {
            return;
        }

        if (!this.mountedByService)
        {
            return;
        }

        _ = RunProcess("net", $"use {this.driveLetter} /delete /y");
        this.mountedByService = false;
    }

    private static int RunProcess(string fileName, string args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    private static bool TryBuildWindowsUncPath(string listenerPrefix, out string uncPath)
    {
        uncPath = string.Empty;
        if (!Uri.TryCreate(listenerPrefix, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hostPart = uri.Host;
        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            hostPart += "@SSL";
        }

        var defaultPort = uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80;
        if (uri.Port != defaultPort)
        {
            hostPart += "@" + uri.Port;
        }

        var nodeSegments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var basePath = @$"\\{hostPart}\DavWWWRoot";
        uncPath = nodeSegments.Length == 0
            ? basePath
            : basePath + "\\" + string.Join("\\", nodeSegments);

        return true;
    }

    private static string NormalizeDriveLetter(string configured)
    {
        var trimmed = configured.Trim();
        if (trimmed.Length == 1 && char.IsLetter(trimmed[0]))
        {
            return char.ToUpperInvariant(trimmed[0]) + ":";
        }

        if (trimmed.Length == 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':')
        {
            return char.ToUpperInvariant(trimmed[0]) + ":";
        }

        return "T:";
    }

    private async Task AcceptLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await this.listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                if (!this.listener.IsListening)
                {
                    break;
                }

                continue;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => this.HandleRequest(context), cancellationToken);
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        response.Headers["DAV"] = "1";
        response.Headers["MS-Author-Via"] = "DAV";

        try
        {
            switch (request.HttpMethod.ToUpperInvariant())
            {
                case "OPTIONS":
                    HandleOptions(response);
                    break;
                case "PROPFIND":
                    await this.HandlePropFind(request, response).ConfigureAwait(false);
                    break;
                case "GET":
                case "HEAD":
                    await this.HandleGetOrHead(request, response).ConfigureAwait(false);
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    response.Headers["Allow"] = "OPTIONS, PROPFIND, GET, HEAD";
                    break;
            }
        }
        catch
        {
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    private static void HandleOptions(HttpListenerResponse response)
    {
        response.StatusCode = (int)HttpStatusCode.OK;
        response.Headers["Allow"] = "OPTIONS, PROPFIND, GET, HEAD";
    }

    private async Task HandleGetOrHead(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (this.root is null)
        {
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            return;
        }

        var path = request.Url?.AbsolutePath ?? "/";
        if (!TryResolveNode(this.root, path, out var node))
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        if (node.NodeType == NodeType.Directory)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/html; charset=utf-8";

            if (request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var html = BuildDirectoryListingHtml(path, node);
            var bytes = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            return;
        }

        await using var buffer = new MemoryStream();
        var stream = await node.Get().ConfigureAwait(false);
        if (stream is null)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        try
        {
            await stream.Open().ConfigureAwait(false);
            ulong offset = 0;

            while (true)
            {
                var chunk = await stream.Read(offset, 16 * 1024).ConfigureAwait(false);
                if (chunk.Length == 0)
                {
                    break;
                }

                await buffer.WriteAsync(chunk, 0, chunk.Length).ConfigureAwait(false);
                offset += (ulong)chunk.Length;
            }
        }
        finally
        {
            stream.Close();
        }

        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "application/octet-stream";
        response.ContentLength64 = buffer.Length;

        if (request.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(response.OutputStream).ConfigureAwait(false);
    }

    private async Task HandlePropFind(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (this.root is null)
        {
            response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            return;
        }

        var path = request.Url?.AbsolutePath ?? "/";
        if (!TryResolveNode(this.root, path, out var node))
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var depth = ParseDepth(request.Headers["Depth"]);
        var nodes = EnumerateNodes(path, node, depth).ToList();

        var doc = BuildMultistatusXml(nodes);
        var payload = Encoding.UTF8.GetBytes(doc.Declaration + doc.ToString(SaveOptions.DisableFormatting));

        response.StatusCode = 207;
        response.ContentType = "application/xml; charset=utf-8";
        response.ContentLength64 = payload.Length;
        await response.OutputStream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);
    }

    private static int ParseDepth(string? depthHeader)
    {
        if (string.IsNullOrWhiteSpace(depthHeader))
        {
            return 1;
        }

        if (depthHeader.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (depthHeader.Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (depthHeader.Equals("infinity", StringComparison.OrdinalIgnoreCase))
        {
            return int.MaxValue;
        }

        return 1;
    }

    private static bool TryResolveNode(INode root, string absolutePath, out INode node)
    {
        var segments = SplitPathSegments(absolutePath);
        var current = root;

        foreach (var segment in segments)
        {
            var next = current.Children?.FirstOrDefault(child =>
                string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase));

            if (next is null)
            {
                node = root;
                return false;
            }

            current = next;
        }

        node = current;
        return true;
    }

    private static IEnumerable<string> SplitPathSegments(string absolutePath)
    {
        return absolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Uri.UnescapeDataString);
    }

    private static IEnumerable<(string href, INode node)> EnumerateNodes(string requestPath, INode rootNode, int depth)
    {
        var normalizedRequestPath = EnsureStartsWithSlash(requestPath);
        var queue = new Queue<(string href, INode node, int level)>();
        queue.Enqueue((normalizedRequestPath, rootNode, 0));

        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            yield return (item.href, item.node);

            if (item.level >= depth || item.node.NodeType != NodeType.Directory)
            {
                continue;
            }

            foreach (var child in item.node.Children ?? Array.Empty<INode>())
            {
                var childHref = CombinePath(item.href, child.Name);
                queue.Enqueue((childHref, child, item.level + 1));
            }
        }
    }

    private static XDocument BuildMultistatusXml(IEnumerable<(string href, INode node)> nodes)
    {
        XNamespace dav = "DAV:";
        var responses = new List<XElement>();

        foreach (var item in nodes)
        {
            var isDirectory = item.node.NodeType == NodeType.Directory;

            var resourceType = isDirectory
                ? new XElement(dav + "resourcetype", new XElement(dav + "collection"))
                : new XElement(dav + "resourcetype");

            var href = isDirectory ? EnsureEndsWithSlash(item.href) : item.href;

            responses.Add(new XElement(
                dav + "response",
                new XElement(dav + "href", href),
                new XElement(
                    dav + "propstat",
                    new XElement(
                        dav + "prop",
                        new XElement(dav + "displayname", item.node.Name),
                        resourceType,
                        new XElement(dav + "getcontenttype", isDirectory ? "httpd/unix-directory" : "application/octet-stream")
                    ),
                    new XElement(dav + "status", "HTTP/1.1 200 OK")
                )
            ));
        }

        return new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(dav + "multistatus", responses)
        );
    }

    private static string BuildDirectoryListingHtml(string absolutePath, INode directory)
    {
        var title = WebUtility.HtmlEncode(absolutePath);
        var sb = new StringBuilder();

        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><title>")
            .Append(title)
            .Append("</title></head><body><h1>INode Browser: ")
            .Append(title)
            .Append("</h1><ul>");

        if (!absolutePath.Equals("/", StringComparison.Ordinal))
        {
            sb.Append("<li><a href=\"")
                .Append(WebUtility.HtmlEncode(ParentPathOf(absolutePath)))
                .Append("\">..</a></li>");
        }

        foreach (var child in directory.Children ?? Array.Empty<INode>())
        {
            var href = CombinePath(absolutePath, child.Name);
            if (child.NodeType == NodeType.Directory)
            {
                href = EnsureEndsWithSlash(href);
            }

            sb.Append("<li><a href=\"")
                .Append(WebUtility.HtmlEncode(href))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(child.Name))
                .Append(child.NodeType == NodeType.Directory ? "/" : string.Empty)
                .Append("</a></li>");
        }

        sb.Append("</ul></body></html>");
        return sb.ToString();
    }

    private static string ParentPathOf(string absolutePath)
    {
        var normalized = EnsureStartsWithSlash(absolutePath).TrimEnd('/');
        if (normalized.Length == 0)
        {
            return "/";
        }

        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            return "/";
        }

        return normalized.Substring(0, lastSlash + 1);
    }

    private static string CombinePath(string basePath, string nodeName)
    {
        var normalized = EnsureStartsWithSlash(basePath).TrimEnd('/');
        var escapedName = Uri.EscapeDataString(nodeName);

        if (normalized == string.Empty)
        {
            return "/" + escapedName;
        }

        return normalized + "/" + escapedName;
    }

    private static string EnsureStartsWithSlash(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        return path.StartsWith('/') ? path : "/" + path;
    }

    private static string EnsureEndsWithSlash(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "/";
        }

        return path.EndsWith('/') ? path : path + "/";
    }

    private static string NormalizePrefix(string configuredPrefix)
    {
        var prefix = configuredPrefix.Trim();
        if (!prefix.EndsWith('/'))
        {
            prefix += "/";
        }

        return prefix;
    }
}
