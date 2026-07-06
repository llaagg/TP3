using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using TP3.Service.FileSystem;
using TP3.Agent.Logic.Transport;
using TP3.Agent.Logic.Host;

[UnsupportedOSPlatform("browser")]
public class ServerManager : IServerManager
{
    private readonly ILogger logger;

    public ServerManager(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task StartServer()
    {
        await StartServerTask();
    }

    public async Task StartServerCmd()
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
            logger?.LogInformation("SERVER: " + args.Data);
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            logger?.LogError("SERVER: " + args.Data);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    
	public async Task StartServerTask()
	{
		var ipcport =5001;
        var ah = new AgentHost(logger, 
                new[] { new FileSystemService() }, 
                new []{ new TP3Transport(logger, new IpcTransport(ipcport, logger))}
        );

        logger.LogInformation("Initializing agent host...");
        await ah.Init();

        logger.LogInformation("Starting agent host...");
        await ah.Start();

        logger.LogInformation("Agent service started. Press Ctrl+C to exit.");
	}

}