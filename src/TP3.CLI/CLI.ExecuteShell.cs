using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

namespace TP3.CLI;

public static partial class CLI
{

    private static async Task ExecuteShell(int ipcPort, int bePatientAndWaitForServer, LogLevel level)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // bump it - debug
        //level = LogLevel.Debug;
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

        // Context context = new Context()
        // {
        //     RootTag = tag,
        //     ipcClient = ipcClient,
        //     logger = logger,
        //     nfo = attachResponse?.AttachResponse.Info
        // };
    
        // go to service folder:
        // shell/control/sh
        // run the command and get the output

        var walk = await ipcClient.Walk(new string[] { "shell", "control", "sh" }, logger);
        walk.ThrowIfError();
        if(walk.WalkResponse?.Infos?.Count() != 3)
        {
            throw new InvalidOperationException("Failed to walk to shell/control/sh");
        }

        while (true)
        {
            var line = Console.ReadLine();

            if (line == null || line.Trim().Length == 0)
            {
                continue;
            }

            try
            {
                ///
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing command: {Command}", line);
            }

        }
    }
}
