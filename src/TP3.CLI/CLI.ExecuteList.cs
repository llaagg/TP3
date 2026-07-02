using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Messages;

namespace TP3.CLI;

public static partial class CLI
{

    private static async Task ExecuteList(int ipcPort, int waitForServer, string? path, ILogger logger)
    {
        logger.LogInformation("Connecting to IPC server on port {IpcPort}", ipcPort);
        var ipcClient = new IpcClient(ipcPort, logger, waitForServer);
        await ipcClient.ConnectAsync();

        var request = MessageHelper.ParseMessage(path != null ? $"walk {path}" : "walk");
        await ipcClient.SendMessageAsync(request);

        var walkMessage = await ReceiveSingleResponse(ipcClient, logger).ConfigureAwait(false);
        
        if (walkMessage is not TP3WalkResponse walkResponse)
        {
            logger.LogWarning("No WALK response received.");
            await ipcClient.DisconnectAsync();
            return;
        }

        var qid = walkResponse.Infos.FirstOrDefault()?.Id;
        var offset = 0L;
        const int maxBytes = 16 * 1024;

        logger.LogInformation(" {Qid}-> Consuming responses from IPC server...", qid);
        while (true)
        {
            var readCommand = MessageHelper.ParseMessage($"read {qid} {offset} {maxBytes}");
            await ipcClient.SendMessageAsync(readCommand);

            var readMessage = await ReceiveSingleResponse(ipcClient, logger).ConfigureAwait(false);
            logger.LogDebug(" {Qid}-> Received response: {ResponseCommand}", qid, readMessage.Command);
            if (readMessage is not TP3ReadResponse response)
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
}
