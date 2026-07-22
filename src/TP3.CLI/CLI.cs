using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace TP3.CLI;

public static partial class CLI
{
    public static Task<int> RunAsync(string[] args)
    {
        var ipcPortOption = new Option<int>(new[] { "--ipc-port", "-i" }, () => 5001, "IPC port to connect to");
        var bePatientAndWaitForServer = new Option<int>(new[] { "--wait-for-server", "-w" }, () => 60, "Wait for the IPC server to be ready before sending messages");
        var path = new Argument<string[]?>("path", () => null, "Path to walk in the IPC server");
        var singlePath = new Argument<string>("path", "Absolute or relative TP3 path");
        var commandPath = new Argument<string>("command-path", "TP3 command node path");
        var commandArgs = new Argument<string[]>("args", () => Array.Empty<string>(), "Arguments passed to the command node");
        var enableEmoted = new Option<bool>(new[] { "--enable-emoted", "-e" }, () => true, "Enable emoted output for file and directory types");
        var logLevel = new Option<LogLevel>("log-level", () => LogLevel.Error, "Log level for the CLI");
        var argsArgument = new Argument<string[]>("args", "Arguments to pass to the command being executed");
        var commndFile = new Argument<string>("command-file", "File containing the command to execute in the webdav mounted folder");

        var listCommand = ListCommand(ipcPortOption, bePatientAndWaitForServer, path, enableEmoted, logLevel);
        listCommand.AddAlias("ls");

        var readCommand = ReadCommand(ipcPortOption, bePatientAndWaitForServer, singlePath, logLevel);
        var runCommand = RunCommand(ipcPortOption, bePatientAndWaitForServer, commandPath, commandArgs, logLevel);

        var rootCommand = new RootCommand("TP3 CLI")
        {
            listCommand, // walk and open on dir
            readCommand, // read 
            runCommand,  // run command
            TermCommand(ipcPortOption, bePatientAndWaitForServer, logLevel), // terminal
        };


        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Specify a command. Use --help for usage details.");
        });

        return rootCommand.InvokeAsync(args);
    }

    private static Command TermCommand(Option<int> ipcPortOption, Option<int> bePatientAndWaitForServer, Option<LogLevel> logLevel)
    {
        var termCommand = new Command("shell", "Start a terminal session with the IPC server")
        {
            ipcPortOption,
            bePatientAndWaitForServer,
            logLevel
        };

        termCommand.SetHandler(ExecuteShell, ipcPortOption, bePatientAndWaitForServer, logLevel);

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
