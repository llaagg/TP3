using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace TP3.CLI;

public static class CommandLineApplication
{
    public static Task<int> RunAsync(string[] args, ILogger logger)
    {
        var portOption = new Option<int>(new[] { "--port", "-p" }, () => 5000, "Port to listen on");
        var ipcPortOption = new Option<int>(new[] { "--ipc-port", "-i" }, () => 5001, "IPC port to connect to");
        var consumeResponses = new Option<bool>(new[] { "--consume-responses", "-c" } , () => true, "Consume responses from the IPC server");
        var bePatientAndWaitForServer = new Option<int>(new[] { "--wait-for-server", "-w" }, ()=>60, "Wait for the IPC server to be ready before sending messages");
        var messageArgument = new Argument<string>("message", "Message to send to IPC server");
        var path = new Argument<string?>("path", () => null ,"Path to walk in the IPC server");

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

    private static async Task ExecuteList(int ipcPort, int waitForServer, string? path, ILogger logger)
    {
        logger.LogInformation("Connecting to IPC server on port {IpcPort}", ipcPort);

        var ipcClient = new IpcClient(ipcPort, logger, waitForServer);
        await ipcClient.ConnectAsync();

        var command = path != null ? $"walk {path}" : "walk";
        logger.LogInformation("Sending message to IPC server: {Message}", command);
        await ipcClient.SendMessageAsync(command);

        var walkResponse = await ReceiveSingleResponse(ipcClient).ConfigureAwait(false);
        if (walkResponse is null)
        {
            logger.LogWarning("No WALK response received.");
            await ipcClient.DisconnectAsync();
            return;
        }

        if (!string.IsNullOrWhiteSpace(walkResponse.Error))
        {
            logger.LogWarning("WALK failed: {Error}", walkResponse.Error);
            await ipcClient.DisconnectAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(walkResponse.Qid))
        {
            logger.LogWarning("WALK response did not include qid.");
            await ipcClient.DisconnectAsync();
            return;
        }

        var qid = walkResponse.Qid;
        var offset = 0L;
        const int maxBytes = 16 * 1024;

        logger.LogInformation("Consuming responses from IPC server...");
        while (true)
        {
            await ipcClient.SendMessageAsync($"read {qid} {offset} {maxBytes}").ConfigureAwait(false);
            var response = await ReceiveSingleResponse(ipcClient).ConfigureAwait(false);
            if (response is null)
            {
                break;
            }

            var payload = response.Data is { Length: > 0 }
                ? System.Text.Encoding.UTF8.GetString(response.Data)
                : string.Empty;

            if (string.Equals(payload, "EOF", StringComparison.Ordinal))
            {
                logger.LogInformation("Received EOF for list qid={Qid}.", qid);
                break;
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                Console.WriteLine(payload);
            }

            offset = response.Offset;
        }

        await ipcClient.DisconnectAsync().ConfigureAwait(false);
    }

    private static async Task ExecteWalk(int ipcPort, bool consumeResponses, int waitForServer, string? path, ILogger logger)
    {
        logger.LogInformation("Connecting to IPC server on port {IpcPort}", ipcPort);

        var ipcClient = new IpcClient(ipcPort, logger, waitForServer);
        await ipcClient.ConnectAsync();

        if (path != null)
        {
            logger.LogInformation("Sending message to IPC server: {Message}", path);
        }

        string command = path != null ? $"walk {path}" : "walk";
        await ipcClient.SendMessageAsync(command);

        // let's wait a bit to allow the server to process the walk command and send responses
        if(consumeResponses)
        {
            logger.LogInformation("Consuming responses from IPC server...");
            // Simulate consuming responses
            await foreach (var response in ipcClient.ListenAsync())
            {
                logger.LogInformation("Received response: {Response}", response);
                if(response.IsFinalChunk)
                {
                    logger.LogInformation("Received final chunk. Stopping response consumption.");
                    break;
                }
            }
            logger.LogInformation("Finished consuming responses.");
        }

        logger.LogInformation("Message sent to IPC server. Disconnecting.");
        await ipcClient.DisconnectAsync();
    }

    private static async Task ExecuteRead(int ipcPort, string message, bool consumeResponses, int waitForServer, ILogger logger)
    {
        logger.LogInformation("Connecting to IPC server on port {IpcPort}", ipcPort);

        var ipcClient = new IpcClient(ipcPort, logger, waitForServer);
        await ipcClient.ConnectAsync();

        logger.LogInformation("Sending message to IPC server: {Message}", message);
        await ipcClient.SendMessageAsync($"read {message}");

        if(consumeResponses)
        {
            logger.LogInformation("Consuming responses from IPC server...");
            // Simulate consuming responses
            await foreach (var response in ipcClient.ListenAsync())
            {
                logger.LogInformation("Received response: {Response}", response);
            }
            logger.LogInformation("Finished consuming responses.");
        }

        logger.LogInformation("Message sent to IPC server. Disconnecting.");
        await ipcClient.DisconnectAsync();
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
