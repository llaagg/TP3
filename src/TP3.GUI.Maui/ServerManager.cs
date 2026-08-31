using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;
using TP3.Agent.Logic.Agent;

[UnsupportedOSPlatform("browser")]
public class ServerManager : IServerManager
{
    private readonly ILogger logger;
    private readonly IService[] services;

    public ServerManager(ILogger logger, IEnumerable<IService> services)
    {
        this.logger = logger;
        this.services = services.ToArray();
    }

    public async Task StartServer()
    {
        await StartServerTask(this.services);
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

    
	public async Task StartServerTask(IService[]? services)
	{
		var ah = new Agent(logger, services);

        logger.LogInformation("Initializing agent host...");
        await ah.Init();

        logger.LogInformation("Starting agent host...");
        await ah.Start();

        logger.LogInformation("Agent service started. Press Ctrl+C to exit.");
	}

}