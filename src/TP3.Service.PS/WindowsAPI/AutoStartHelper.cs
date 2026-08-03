using Microsoft.Win32;
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
}