using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

namespace TP3.CLI;

public static partial class CLI
{

    private static async Task ExecuteTerm(int ipcPort, int bePatientAndWaitForServer, LogLevel level)
    {
        // bump it - debug
        level = LogLevel.Debug;
        var logger = InitilizeLogger(level);
        logger.LogInformation("Starting terminal session with IPC server on port {IpcPort}", ipcPort);
        
        // attaching to the IPC server
        var client = new TP3Client(ipcPort, logger);
        var ipcClient = new TP3Client(ipcPort, logger, bePatientAndWaitForServer);
        await ipcClient.ConnectAsync();
        var attachResponse = await ipcClient.Attach(logger).ConfigureAwait(false);
        attachResponse.ThrowIfError();
        var tag = attachResponse?.Tag;

        // we are connected with tag
        logger.LogInformation("Connected to IPC server with tag {Tag}", tag);
        var walkRequest = new TP3WalkRequest();

        while(true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line == null)
            {
                break;
            }
            logger.LogInformation("Read line: {Line}", line);
        }
    }
}
