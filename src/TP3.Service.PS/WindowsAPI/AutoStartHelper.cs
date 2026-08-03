using Microsoft.Win32;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Base;

public static class AutoStartHelper
{
    public static IEnumerable<AutoStartApp> GetAutoStartApps()
    {

        const string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        using (RegistryKey startupKey = Registry.LocalMachine.OpenSubKey(runKey))
        {
            var valueNames = startupKey.GetValueNames();

            // Name => File path
            var appInfos = valueNames
                .Where(valueName => startupKey.GetValueKind(valueName) == RegistryValueKind.String)
                .ToDictionary(valueName => valueName, valueName => startupKey.GetValue(valueName).ToString());

            return appInfos.Select(kvp => new AutoStartApp(kvp.Key, kvp.Value, true));
        }
    }
}

public class AutoStartApp : BaseDirectoryNode
{
    public string Name { get; }
    public string Path { get; }
    public bool Enabled { get; }

    public AutoStartApp(string name, string path, bool enabled)
        : base(name)
    {
        Name = name;
        Path = path;
        Enabled = enabled;
    }

    public override IEnumerable<INode>? Children => new List<INode>()
    {
        new AutoStartAppEnabled(this),
    };
}

public class AutoStartAppEnabled : INode
{
    private AutoStartApp parent;

    public AutoStartAppEnabled(AutoStartApp parent)
    {
        this.parent = parent;
    }

    public string Id => "enabled";

    public string Name => "enabled";

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => null;

    public ulong Length => 0;

    public async Task<ITP3DataStream?> Get()
    {
        var enabledValue = parent.Enabled ? "true" : "false";
        return await Task.FromResult<ITP3DataStream?>(new TP3Stream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(enabledValue))));
    }
}