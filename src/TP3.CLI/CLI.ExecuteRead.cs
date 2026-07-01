using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Messages;

namespace TP3.CLI;

public static partial class CLI
{
    private static async Task ExecuteRead(int ipcPort, string message, bool consumeResponses, int waitForServer, ILogger logger)
    {
        logger.LogInformation("Connecting to IPC server on port {IpcPort}", ipcPort);

        var ipcClient = new IpcClient(ipcPort, logger, waitForServer);
        await ipcClient.ConnectAsync();

        var request = MessageHelper.ParseMessage($"read {message}");
        await ipcClient.SendMessageAsync(request);
        
        if (consumeResponses)
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
}
