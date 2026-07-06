using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

[UnsupportedOSPlatform("browser")]
public class ServerManager
{
    private readonly ILogger? _logger;

    public ServerManager(ILogger? logger = null)
    {
        _logger = logger;
    }

    public void StartServer()
    {
        string path = "C:\\Work\\Sandbox\\TP3\\publish\\TP3.Server\\TP3.Server.exe";

        var process = new Process();

        process.StartInfo.FileName = path;
        process.StartInfo.Arguments = "run";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.OutputDataReceived += (sender, args) =>
        {
            _logger?.LogInformation("SERVER: " + args.Data);
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            _logger?.LogError("SERVER: " + args.Data);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }
}