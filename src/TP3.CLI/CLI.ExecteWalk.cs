using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Messages;

namespace TP3.CLI;

public static partial class CLI
{
    private static async Task ExecteWalk(int ipcPort, bool consumeResponses, int waitForServer, string? path, ILogger logger)
    {
        logger.LogInformation("Connecting to IPC server on port {IpcPort}", ipcPort);

        var ipcClient = new IpcClient(ipcPort, logger, waitForServer);
        await ipcClient.ConnectAsync();

        if (path != null)
        {
            logger.LogInformation("Sending message to IPC server: {Message}", path);
        }

        var request = MessageHelper.ParseMessage(path != null ? $"walk {path}" : "walk");
        await ipcClient.SendMessageAsync(request);

        // let's wait a bit to allow the server to process the walk command and send responses
        if (consumeResponses)
        {
            logger.LogInformation("Consuming responses from IPC server...");
            // Simulate consuming responses
            await foreach (var response in ipcClient.ListenAsync())
            {
                logger.LogInformation("Received response: {Response}", response);
                if (response.PayloadCase == TP3Message.PayloadOneofCase.ReadResponse)
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
}
