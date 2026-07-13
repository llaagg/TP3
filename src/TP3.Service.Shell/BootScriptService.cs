using TP3.Interfaces;

namespace TP3.Service.Shell;

public class BootScriptService : BaseDirectoryNode, IService
{
    private readonly ShellService shellService;
    private readonly string bootFilePath;

    public BootScriptService(ShellService shellService, string bootFilePath)
        : base("boot")
    {
        this.shellService = shellService;
        this.bootFilePath = bootFilePath;
    }

    public Task Init(IAgent me)
    {
        EnsureBootFileExists(this.bootFilePath);
        return Task.CompletedTask;
    }

    public async Task Start()
    {
        if (!File.Exists(this.bootFilePath))
        {
            return;
        }

        var lines = await File.ReadAllLinesAsync(this.bootFilePath).ConfigureAwait(false);
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            _ = await this.shellService.ExecuteCommandAsync(line).ConfigureAwait(false);
        }
    }

    public Task Stop()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    public override IEnumerable<INode>? Children =>
    [
        new TP3.Protocol.StreamNode(() => this.bootFilePath, "path")
    ];

    private static void EnsureBootFileExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(path))
        {
            return;
        }

        File.WriteAllText(path, "# TP3 boot commands\n# one command per line\n# available: create, ls, cd, sh\n# example:\n# create\n");
    }
}
