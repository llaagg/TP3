using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TP3.Messages;

namespace TP3.CLI;

public static partial class CLI
{
    public static ILogger InitilizeLogger(LogLevel logLevel)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                })
                .SetMinimumLevel(logLevel);
        });

        var logger = loggerFactory.CreateLogger(Assembly.GetExecutingAssembly().GetName().Name!);
        return logger;
    }

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

        // List folder on the IPC server
        var listCommand = new Command("list", "List the contents of a folder in the IPC server")
        {
            logLevel,
            ipcPortOption,
            bePatientAndWaitForServer,
            enableEmoted,
            path,            
        };
        listCommand.SetHandler(async (int ipcPort, int waitForServer, bool enableEmoted, LogLevel logLevel, string[]? path) =>
            await ExecuteList(ipcPort, waitForServer, enableEmoted, logLevel, path),
                ipcPortOption, bePatientAndWaitForServer, enableEmoted, logLevel, path);

        var rootCommand = new RootCommand("TP3 CLI")
        {
            listCommand
        };

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Specify a command. Use --help for usage details.");
        });

        return rootCommand.InvokeAsync(args);
    }

    private static async Task<TP3.Messages.TP3Message?> ReceiveSingleResponse(TP3Client ipcClient, ILogger logger)
    {
        logger.LogInformation("Listening for a single response from the IPC server...");
        await foreach (var response in ipcClient.ListenAsync())
        {
            return response;
        }

        return null;
    }
}
