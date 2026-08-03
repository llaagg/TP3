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
        return new RWStreamString(
            readFunc: async () =>
            {
                return parent.Enabled ? "1" : "0";
            },
            writeFunc: async (value) =>
            {
               throw new NotImplementedException("Writing to the enabled property is not implemented."); 
            });
    }
}

public abstract class RWStream : ITP3DataStream
{
    public uint Iounit => 0;

    public ulong Position { get; protected set; } = 0;

    public void Close()
    {
    }

    public async Task Open()
    {
        
    }

    public async Task<byte[]> Read(ulong offset, ulong maxCount)
    {
        return await OnRead(offset, maxCount);
    }

    public async Task<ulong> Write(ulong offset, byte[] data)
    {
        var incomingData = data;
        await OnWrite(incomingData);
        return (ulong)data.Length;
    }

    public virtual async Task OnWrite(byte[] data)
    {
        
    }

    public virtual async Task<byte[]> OnRead(ulong offset, ulong maxCount)
    {
        return new byte[0];
    }
}

public class RWStreamString : RWStream
{
    public RWStreamString(Func<Task<string>> readFunc, Func<string, Task> writeFunc)
    {
        this.readFunc = readFunc;
        this.writeFunc = writeFunc;
    }

    private Func<Task<string>> readFunc;
    private Func<string, Task> writeFunc;

    public override async Task OnWrite(byte[] data)
    {
        var incomingData = System.Text.Encoding.UTF8.GetString(data);
        await writeFunc(incomingData);
    }

    public override async Task<byte[]> OnRead(ulong offset, ulong maxCount)
    {
        if(offset != 0)
        {
            return new byte[0];
        }

        var result = await readFunc();
        this.Position = (ulong)result.Length;
        return System.Text.Encoding.UTF8.GetBytes(result);
    }
}