using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Transport;
using TP3.Service.Attached;
using TP3.Service.FileSystem;
using TP3.Service.IPC;

namespace TP3.CLI;

public static class CommandLineApplication
{
    public static Task<int> RunAsync(string[] args, ILogger logger)
    {
        var portOption = new Option<int>(new[] { "--port", "-p" }, () => 5000, "Port to listen on");
        var ipcPortOption = new Option<int>(new[] { "--ipc-port", "-i" }, () => 5001, "IPC port to connect to or listen on");
        var messageArgument = new Argument<string>("message", "Message to send to IPC server");

        var runCommand = new Command("run", "Start the agent host")
        {
            portOption,
            ipcPortOption
        };
        runCommand.SetHandler(async (int port, int ipcPort) => await ExecuteStart(port, ipcPort, logger), portOption, ipcPortOption);

        var rootCommand = new RootCommand("TP3 CLI")
        {
            runCommand
        };

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Specify a command. Use --help for usage details.");
        });

        return rootCommand.InvokeAsync(args);
    }

    private static async Task ExecuteStart(int port, int ipcPort, ILogger logger)
    {
        logger.LogInformation("Starting agent service on port {Port} with IPC on port {IpcPort}.", port, ipcPort);

        var ah = new TP3.Agent.Logic.Agent.Agent(logger,
                    new IService[]
                    {
                        new FileSystemService(),
                        new IpcService(ipcPort, logger),
                        new AttachedService(port, logger)
                    }
        );

        logger.LogInformation("Initializing agent host...");
        await ah.Init();

        logger.LogInformation("Starting agent host...");
        await ah.Start();

        logger.LogInformation("Agent service started. Press Ctrl+C to exit.");
    }
}
