using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace TP3.CLI;

public static partial class CLI
{
    public static Task<int> RunAsync(string[] args, ILogger logger)
    {
        var portOption = new Option<int>(new[] { "--port", "-p" }, () => 5000, "Port to listen on");
        var ipcPortOption = new Option<int>(new[] { "--ipc-port", "-i" }, () => 5001, "IPC port to connect to");
        var consumeResponses = new Option<bool>(new[] { "--consume-responses", "-c" }, () => true, "Consume responses from the IPC server");
        var bePatientAndWaitForServer = new Option<int>(new[] { "--wait-for-server", "-w" }, () => 60, "Wait for the IPC server to be ready before sending messages");
        var messageArgument = new Argument<string>("message", "Message to send to IPC server");
        var path = new Argument<string?>("path", () => null, "Path to walk in the IPC server");

        // READ
        var readCommand = new Command("read", "Read a message from the IPC server")
        {
            ipcPortOption,
            consumeResponses,
            bePatientAndWaitForServer,
            messageArgument,
        };
        readCommand.SetHandler(async (int ipcPort, string message, bool consumeResponses, int waitForServer) =>
            await ExecuteRead(ipcPort, message, consumeResponses, waitForServer, logger),
                ipcPortOption, messageArgument, consumeResponses, bePatientAndWaitForServer);

        // WALK
        var walkCommand = new Command("walk", "Walk a path in the IPC server")
        {
            ipcPortOption,
            bePatientAndWaitForServer,
            consumeResponses,
            path
        };
        walkCommand.SetHandler(async (int ipcPort, int waitForServer, bool consumeResponses, string? path) =>
            await ExecteWalk(ipcPort, consumeResponses, waitForServer, path, logger),
                ipcPortOption, bePatientAndWaitForServer, consumeResponses, path);


        // List folder on the IPC server
        var listCommand = new Command("list", "List the contents of a folder in the IPC server")
        {
            ipcPortOption,
            bePatientAndWaitForServer,
            path
        };
        listCommand.SetHandler(async (int ipcPort, int waitForServer, string? path) =>
            await ExecuteList(ipcPort, waitForServer, path, logger),
                ipcPortOption, bePatientAndWaitForServer, path);

        var rootCommand = new RootCommand("TP3 CLI")
        {
            readCommand,
            walkCommand,
            listCommand
        };

        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("Specify a command. Use --help for usage details.");
        });

        return rootCommand.InvokeAsync(args);
    }

    private static async Task<TP3.Messages.TP3Message?> ReceiveSingleResponse(IpcClient ipcClient)
    {
        await foreach (var response in ipcClient.ListenAsync())
        {
            return response;
        }

        return null;
    }
}
