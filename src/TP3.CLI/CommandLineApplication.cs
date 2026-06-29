using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;

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
        runCommand.SetHandler((int port, int ipcPort) => ExecuteStart(port, ipcPort, logger), portOption, ipcPortOption);

        var startCommand = new Command("start", "Start the agent host (alias)")
        {
            portOption,
            ipcPortOption
        };
        startCommand.SetHandler((int port, int ipcPort) => ExecuteStart(port, ipcPort, logger), portOption, ipcPortOption);

        var echoCommand = new Command("echo", "Send an ECHO message to the local IPC server")
        {
            ipcPortOption,
            messageArgument
        };
        echoCommand.SetHandler(async (int ipcPort, string message) => await ExecuteEchoAsync(ipcPort, message, logger), ipcPortOption, messageArgument);

        var rootCommand = new RootCommand("TP3 CLI")
        {
            runCommand,
            startCommand,
            echoCommand
        };

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Specify a command. Use --help for usage details.");
        });

        return rootCommand.InvokeAsync(args);
    }

    private static void ExecuteStart(int port, int ipcPort, ILogger logger)
    {
        logger.LogInformation("Starting agent service on port {Port} with IPC on port {IpcPort}.", port, ipcPort);
        AgentHost.Main(Array.Empty<string>(), port, ipcPort, logger);
    }

    private static async Task ExecuteEchoAsync(int ipcPort, string message, ILogger logger)
    {
        logger.LogInformation("Sending ECHO to IPC port {IpcPort}: {Message}", ipcPort, message);
        var response = await IpcClient.SendAsync("127.0.0.1", ipcPort, $"ECHO {message}").ConfigureAwait(false);
        Console.WriteLine(response);
    }
}
