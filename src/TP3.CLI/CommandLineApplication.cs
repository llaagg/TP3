using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Service.FileSystem;

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

        var echoCommand = new Command("echo", "Send an ECHO message to the local IPC server")
        {
            ipcPortOption,
            messageArgument
        };
        echoCommand.SetHandler(async (int ipcPort, string message) => await ExecuteEchoAsync(ipcPort, message, logger), ipcPortOption, messageArgument);

        var rootCommand = new RootCommand("TP3 CLI")
        {
            runCommand,
            echoCommand
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

        var ah = new AgentHost(port, ipcPort, logger, new []{new FileSystemService()});
        await ah.Start();

        logger.LogInformation("Agent service started. Press Ctrl+C to exit.");

        CancellationTokenSource cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) => {
            logger.LogInformation("Stopping agent service...");
            ah.Stop();
            logger.LogInformation("Agent service stopped.");
            logger.LogInformation("Stopping IPC connection...");
            cts.Cancel();
            logger.LogInformation("IPC connection stopped.");
        };       
        
        var cliClinet = new CliClient();
        cliClinet.CliInternalClient(new IpcClient("127.0.0.1", ipcPort)).Wait(cts.Token);
    }

    private static async Task ExecuteEchoAsync(int ipcPort, string message, ILogger logger)
    {
        logger.LogInformation("Sending ECHO to IPC port {IpcPort}: {Message}", ipcPort, message);
        var response = await new IpcClient("127.0.0.1", ipcPort).SendAsync( $"ECHO {message}").ConfigureAwait(false);
        Console.WriteLine(response);
    }
}
