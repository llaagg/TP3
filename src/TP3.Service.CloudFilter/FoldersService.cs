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

    public FoldersService()
        : base("folders")
    {
    }

    public void Dispose()
    {
    }

    public async Task Init(IAgent me)
    {
        if (!StorageProviderSyncRootManager.IsSupported())
        {
            throw new PlatformNotSupportedException("Cloud Files API is not supported on this Windows installation.");
        }

        var rootPath = ResolveSyncRootPath();
        Directory.CreateDirectory(rootPath);

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

    public async Task Start()
    {
    }

    public async Task Stop()
    {
    }

    private static string ResolveSyncRootPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(SyncRootPathEnvVar);

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var defaultRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TP3", "MyCloudFolder");
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

    private static async Task MirrorNodeTreeAsync(INode rootNode, string rootPath)
    {
        if (rootNode.NodeType == NodeType.Directory)
        {
            await MirrorChildrenAsync(rootNode, rootPath);
            return;
        }

        var rootFilePath = Path.Combine(rootPath, SafeName(rootNode.Name));
        await WriteNodeFileAsync(rootNode, rootFilePath);
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

    private static async Task MirrorChildrenAsync(INode node, string currentPath)
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
                await MirrorChildrenAsync(child, targetPath);
                continue;
            }

            await WriteNodeFileAsync(child, targetPath);
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
