using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Client;

namespace TP3.CLI;

public static partial class CLI
{

    private static async Task ExecuteList(int ipcPort, int waitForServer, bool enableEmoted, Microsoft.Extensions.Logging.LogLevel logLevel, string[]? path)
    {
        var logger = InitilizeLogger(logLevel);
        if(enableEmoted)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        logger.LogInformation("Connecting to IPC server on port {IpcPort}", ipcPort);
        var ipcClient = new TP3Client(ipcPort, logger, waitForServer);
        await ipcClient.ConnectAsync();
        var attachResponse = await ipcClient.Attach(logger).ConfigureAwait(false);
        attachResponse.ThrowIfError();
        var tag = attachResponse?.Tag;

        logger.LogInformation("Preparing to send list request for path: {Path}", path ?? new string[] { "/" });
        var walkRequest = new TP3WalkRequest();
        if(path != null && path.Length > 0)
        {
            walkRequest.Path.Add(path);
        }
        var walkResponse1 = await ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = tag,
            WalkRequest = walkRequest
        }, logger).ConfigureAwait(false);
        walkResponse1.ThrowIfError();

        var openResponse = await ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = tag,
            OpenRequest = new TP3OpenRequest()
        }, logger).ConfigureAwait(false);
        openResponse.ThrowIfError();

        if(openResponse?.Tag is null)
        {
            throw new InvalidOperationException("Failed to open directory for listing.");
        }

        foreach (var item in ipcClient.ReadDirectory(openResponse, logger))
        {
            if(enableEmoted)
            {
                var empote = item.Info.NodeType == NodeType.Directory ? "📁" : "📄";
                Console.WriteLine($"{empote} {item.Name}");
            }
            else
            {
                Console.WriteLine($"{item.Name}");
            }
        }

        await ipcClient.DisconnectAsync().ConfigureAwait(false);
    }
    
    

    


    

}
