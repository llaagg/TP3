using Microsoft.Win32;
using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Base;



/*
Registry: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run

The values below HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run 
can be used to enable or disable the corresponding values under 
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run.

A value of 02 00 00 … or 06 00 00 … seems to indicate that the entry is enabled, all(?) 
other values that it is disabled. (Possibly, in the case of disabledness, the value is the timestamp of the disabling).

These values can be modified in the startup tab of taskmgr.exe.

See also
The corresponding key for all users is:
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run.


Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run]
"MicrosoftEdgeAutoLaunch_14DA42686A33801FBA0440A5992A3729"=hex:03,00,00,00,e4,\
  7c,77,ac,ef,f3,dc,01
"Teams"=hex:02,00,00,00,00,00,00,00,00,00,00,00
"OneDrive"=hex:02,00,00,00,00,00,00,00,00,00,00,00
"Docker Desktop"=hex:03,00,00,00,00,00,00,00,00,00,00,00
"KeePassXC"=hex:02,00,00,00,00,00,00,00,00,00,00,00
"NordVPN"=hex:01,00,00,00,b1,04,86,8e,d7,f4,dc,01
"Mozilla-Zen-F0DC299D809B9700"=hex:01,00,00,00,eb,44,4a,bd,d4,18,dd,01
"Dawn Launcher"=hex:02,00,00,00,00,00,00,00,00,00,00,00
"Microsoft.Lists"=hex:02,00,00,00,00,00,00,00,00,00,00,00
"Discord"=hex:02,00,00,00,00,00,00,00,00,00,00,00

*/

public static class AutoStartHelper
{
    public static IEnumerable<INode> GetAutoStartApps()
    {
        return new List<INode>()
        {
            new BaseDirectoryNode("user", null, GetStartApps("user").ToList()),
            new BaseDirectoryNode("user-startup-folder", null, GetStartApps("user-startup-folder", "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder").ToList()),
            new BaseDirectoryNode("machine", null, GetStartApps("machine").ToList()),
            new BaseDirectoryNode("all-users", null, GetStartApps("machine").ToList()),
        };
    }

    public static IEnumerable<INode> GetStartApps(string scope = "user", string key = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run")
    {
        using (RegistryKey startupKey = GetReg(scope, key))
        {
            var valueNames = startupKey.GetValueNames();
            //02 00 00 00 or 06 00 00 00: Startup item is Enabled (managed by user).
            //03 00 00 00 or 01 00 00 00: Startup item is Disabled (by the user or system).
            //08 00 00 00: Startup item is Enabled, but locked so the user cannot turn it of
            
            foreach (var valueName in valueNames)
            {
                var value = startupKey.GetValue(valueName);
                var valueKind = startupKey.GetValueKind(valueName);

                if (valueKind == RegistryValueKind.Binary && value is byte[] bytes && bytes.Length >= 4)
                {
                    string path = GetReg(scope, "Software\\Microsoft\\Windows\\CurrentVersion\\Run")?.GetValue(valueName)?.ToString() ?? string.Empty;

                    yield return new AutoStartAppNode(scope, valueName, path)
                    {
                    };
                }
            }
        }
    }

    public static RegistryKey GetReg(string scope, string name, bool writable = false)
    {
        var root = scope == "machine" ? Registry.LocalMachine! : Registry.CurrentUser!;
        return root.OpenSubKey(name);
    }
}

public class AutoStartAppNode : BaseDirectoryNode
{
    public string Scope { get; }
    public string Path { get; }

    public bool Enabled {
        get
        {
            //02 00 00 00 or 06 00 00 00: Startup item is Enabled (managed by user).
            //03 00 00 00 or 01 00 00 00: Startup item is Disabled (by the user or system).
            //08 00 00 00: Startup item is Enabled, but locked so the user cannot turn it of
            var value = AutoStartHelper.GetReg(Scope, Path)?.GetValue(Name);
            if (value is byte[] bytes && bytes.Length >= 4)
            {
                if (bytes[0] == 0x02 || bytes[0] == 0x06 || bytes[0] == 0x08)
                {
                    return true;
                }
            }

            return false;   
        }
        set
        {
            // change registry to enabled for this record
            var startupKey = AutoStartHelper.GetReg(Scope, Path, true);
            if (startupKey != null)
            {
                if (value)
                {
                    // Set to enabled (02 00 00 00)
                    startupKey.SetValue(Name, new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  }, RegistryValueKind.Binary);
                }
                else
                {
                    // Set to disabled (03 00 00 00)
                    startupKey.SetValue(Name, new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                }
            }
        }
    }

    public AutoStartAppNode(string scope, string name, string path)
        : base(name)
    {
        Scope = scope;
        Path = path;
    }

    public override IEnumerable<INode>? Children => new List<INode>()
    {
        new AutoStartAppEnabled((AutoStartAppNode)this),
    };

    internal void SetEnabled(bool v)
    {
        this.Enabled = v;
    }
}

public class AutoStartAppEnabled : INode
{
    private AutoStartAppNode parent;

    public AutoStartAppEnabled(AutoStartAppNode parent)
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
        return new RWStreamString(
            readFunc: async () =>
            {
                return parent.Enabled ? true.ToString() : false.ToString();
            },
            writeFunc: async (value) =>
            {
                // change registry to disabled for this record
                parent.SetEnabled(value == true.ToString());
            });
    }
}
