using TP3.Interfaces;
using TP3.Messages;
using Windows.Storage;
using Windows.Storage.Provider;

namespace TP3.Service.CloudFilter;

public class FoldersService : BaseDirectoryNode, IService
{
    private const string SyncRootPathEnvVar = "TP3_CLOUDFILTER_ROOT";
    private const string SyncRootIdEnvVar = "TP3_CLOUDFILTER_ID";
    private const string SyncRootDisplayNameEnvVar = "TP3_CLOUDFILTER_DISPLAY_NAME";
    private const string SyncRootIconEnvVar = "TP3_CLOUDFILTER_ICON";
    private const string SyncRootNodePathEnvVar = "TP3_CLOUDFILTER_NODE_PATH";

    private const string DefaultSyncRootId = "TP3.CloudFilter!DefaultUser";
    private const string DefaultDisplayName = "TP3 Cloud Drive";
    private const string DefaultNodePath = "/root";

    private readonly Dictionary<string, INode> lazyFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim hydrateGate = new(1, 1);
    private FileSystemWatcher? watcher;
    private string? syncRootPath;

    public FoldersService()
        : base("folders")
    {
    }

    public void Dispose()
    {
        this.watcher?.Dispose();
        this.hydrateGate.Dispose();
    }

    public async Task Init(IAgent me)
    {
        if (!StorageProviderSyncRootManager.IsSupported())
        {
            throw new PlatformNotSupportedException("Cloud Files API is not supported on this Windows installation.");
        }

        var rootPath = ResolveSyncRootPath();
        this.syncRootPath = rootPath;
        Directory.CreateDirectory(rootPath);
        this.lazyFiles.Clear();

        var nodePath = Environment.GetEnvironmentVariable(SyncRootNodePathEnvVar) ?? DefaultNodePath;
        var nodeToMap = ResolveNodeForMapping(me.T, nodePath);

        if (nodeToMap == null)
        {
            throw new InvalidOperationException($"Cannot map node path '{nodePath}' from trunk.");
        }

        await MirrorNodeTreeAsync(nodeToMap, rootPath);

        var syncRootId = Environment.GetEnvironmentVariable(SyncRootIdEnvVar) ?? DefaultSyncRootId;
        var displayName = Environment.GetEnvironmentVariable(SyncRootDisplayNameEnvVar) ?? DefaultDisplayName;
        var iconResource = Environment.GetEnvironmentVariable(SyncRootIconEnvVar) ?? string.Empty;

        await UnregisterExistingSyncRootAsync(syncRootId);

        StorageProviderSyncRootInfo info = new StorageProviderSyncRootInfo
        {
            Id = syncRootId,
            Path = await StorageFolder.GetFolderFromPathAsync(rootPath),
            DisplayNameResource = displayName,
            IconResource = iconResource,
            HydrationPolicy = StorageProviderHydrationPolicy.Progressive,
            PopulationPolicy = StorageProviderPopulationPolicy.AlwaysFull
        };

        StorageProviderSyncRootManager.Register(info);

    }

    public Task Start()
    {
        if (string.IsNullOrWhiteSpace(this.syncRootPath) || !Directory.Exists(this.syncRootPath))
        {
            return Task.CompletedTask;
        }

        if (this.watcher != null)
        {
            return Task.CompletedTask;
        }

        this.watcher = new FileSystemWatcher(this.syncRootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastAccess | NotifyFilters.CreationTime
        };

        this.watcher.Changed += this.OnPathTouched;
        this.watcher.Created += this.OnPathTouched;
        this.watcher.Renamed += this.OnPathRenamed;
        this.watcher.EnableRaisingEvents = true;

        return Task.CompletedTask;
    }

    public Task Stop()
    {
        if (this.watcher == null)
        {
            return Task.CompletedTask;
        }

        this.watcher.EnableRaisingEvents = false;
        this.watcher.Changed -= this.OnPathTouched;
        this.watcher.Created -= this.OnPathTouched;
        this.watcher.Renamed -= this.OnPathRenamed;
        this.watcher.Dispose();
        this.watcher = null;

        return Task.CompletedTask;
    }

    private static string ResolveSyncRootPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(SyncRootPathEnvVar);

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var defaultRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TP3");
        return defaultRoot;
    }

    private static async Task UnregisterExistingSyncRootAsync(string syncRootId)
    {
        var existing = StorageProviderSyncRootManager.GetCurrentSyncRoots();
        if (existing.Any(r => r.Id == syncRootId))
        {
            StorageProviderSyncRootManager.Unregister(syncRootId);
        }

        await Task.CompletedTask;
    }

    private async Task MirrorNodeTreeAsync(INode rootNode, string rootPath)
    {
        if (rootNode.NodeType == NodeType.Directory)
        {
            await this.MirrorChildrenAsync(rootNode, rootPath);
            return;
        }

        var rootFilePath = Path.Combine(rootPath, SafeName(rootNode.Name));
        this.CreatePlaceholder(rootNode, rootFilePath);
    }

    private static INode? ResolveNodeForMapping(INode trunk, string requestedPath)
    {
        var normalizedPath = (requestedPath ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            normalizedPath = "/";
        }

        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        if (normalizedPath.Equals("/", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Equals("/root", StringComparison.OrdinalIgnoreCase))
        {
            return trunk;
        }

        if (normalizedPath.StartsWith("/root/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("/root".Length);
        }

        var segments = normalizedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        INode current = trunk;
        foreach (var segment in segments)
        {
            var children = current.Children;
            if (children == null)
            {
                return null;
            }

            var next = children.FirstOrDefault(child =>
                string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase));

            if (next == null)
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    private async Task MirrorChildrenAsync(INode node, string currentPath)
    {
        var children = node.Children;
        if (children == null)
        {
            return;
        }

        foreach (var child in children)
        {
            var targetName = SafeName(child.Name);
            var targetPath = Path.Combine(currentPath, targetName);

            if (child.NodeType == NodeType.Directory)
            {
                Directory.CreateDirectory(targetPath);
                await this.MirrorChildrenAsync(child, targetPath);
                continue;
            }

            this.CreatePlaceholder(child, targetPath);
        }
    }

    private void CreatePlaceholder(INode node, string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        var parent = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        if (!File.Exists(normalizedPath))
        {
            using var _ = File.Create(normalizedPath);
        }

        this.lazyFiles[normalizedPath] = node;
    }

    private void OnPathTouched(object sender, FileSystemEventArgs e)
    {
        _ = this.TryHydrateAsync(e.FullPath);
    }

    private void OnPathRenamed(object sender, RenamedEventArgs e)
    {
        _ = this.TryHydrateAsync(e.FullPath);
    }

    private async Task TryHydrateAsync(string path)
    {
        var normalizedPath = Path.GetFullPath(path);

        if (!this.lazyFiles.ContainsKey(normalizedPath))
        {
            return;
        }

        await this.hydrateGate.WaitAsync();
        try
        {
            if (!this.lazyFiles.TryGetValue(normalizedPath, out var node))
            {
                return;
            }

            if (!File.Exists(normalizedPath))
            {
                this.lazyFiles.Remove(normalizedPath);
                return;
            }

            var info = new FileInfo(normalizedPath);
            if (info.Length > 0)
            {
                this.lazyFiles.Remove(normalizedPath);
                return;
            }

            await WriteNodeFileAsync(node, normalizedPath);
            this.lazyFiles.Remove(normalizedPath);
        }
        finally
        {
            this.hydrateGate.Release();
        }
    }

    private static async Task WriteNodeFileAsync(INode node, string filePath)
    {
        await using var output = File.Create(filePath);
        var dataStream = await node.Get();
        if (dataStream == null)
        {
            return;
        }

        await dataStream.Open();

        try
        {
            ulong offset = 0;

            while (true)
            {
                var data = await dataStream.Read(offset, 64 * 1024);
                if (data.Length == 0)
                {
                    break;
                }

                await output.WriteAsync(data);
                offset += (ulong)data.Length;
            }
        }
        finally
        {
            dataStream.Close();
        }
    }

    private static string SafeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "unnamed";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }
}
