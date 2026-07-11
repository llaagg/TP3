using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TP3.Messages;

namespace TP3.CLI;

public static partial class CLI
{
    public static Task<int> RunAsync(string[] args)
    {
        var portOption = new Option<int>(new[] { "--port", "-p" }, () => 5000, "Port to listen on");
        var ipcPortOption = new Option<int>(new[] { "--ipc-port", "-i" }, () => 5001, "IPC port to connect to");
        var consumeResponses = new Option<bool>(new[] { "--consume-responses", "-c" }, () => true, "Consume responses from the IPC server");
        var bePatientAndWaitForServer = new Option<int>(new[] { "--wait-for-server", "-w" }, () => 60, "Wait for the IPC server to be ready before sending messages");
        var messageArgument = new Argument<string>("message", "Message to send to IPC server");
        var path = new Argument<string[]?>("path", () => null, "Path to walk in the IPC server");
        var enableEmoted = new Option<bool>(new[] { "--enable-emoted", "-e" }, () => true, "Enable emoted output for file and directory types");
        var logLevel = new Option<LogLevel>("log-level", () => LogLevel.Error, "Log level for the CLI");

        var rootCommand = new RootCommand("TP3 CLI")
        {
            ListCommand(ipcPortOption, bePatientAndWaitForServer, path, enableEmoted, logLevel),
            TermCommand(ipcPortOption, bePatientAndWaitForServer, logLevel)
        };

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Specify a command. Use --help for usage details.");
        });

        return rootCommand.InvokeAsync(args);
    }

    private static Command TermCommand(Option<int> ipcPortOption, Option<int> bePatientAndWaitForServer, Option<LogLevel> logLevel)
    {
        var termCommand = new Command("term", "Start a terminal session with the IPC server")
        {
            ipcPortOption,
            bePatientAndWaitForServer,
            logLevel
        };

        termCommand.SetHandler(ExecuteTerm, ipcPortOption, bePatientAndWaitForServer, logLevel);

        return termCommand;
    }

    private static Command ListCommand(Option<int> ipcPortOption, 
        Option<int> bePatientAndWaitForServer, 
        Argument<string[]?> path, 
        Option<bool> enableEmoted, 
        Option<LogLevel> logLevel)
    {
        var listCommand = new Command("list", "List the contents of a folder in the IPC server")
        {
            logLevel,
            ipcPortOption,
            bePatientAndWaitForServer,
            enableEmoted,
            path,
        };
        listCommand.SetHandler(ExecuteList, ipcPortOption, bePatientAndWaitForServer, enableEmoted, logLevel, path);
        return listCommand;
    }

}
