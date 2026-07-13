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

        var attachResponse = await ipcClient.Attach(logger);
        attachResponse.ThrowIfError();
        var roooTag = attachResponse?.Tag;

        // we are connected with tag
        logger.LogInformation("Connected to IPC server with tag {Tag}", roooTag);

        // go to service folder:
        // shell/control/sh
        // run the command and get the output
        var shPath = new string[] { "services", "shell", "control", "sh" };
        var walk = await ipcClient.Walk(roooTag, shPath, logger);
        walk.ThrowIfError();
        if( walk.WalkResponse?.Infos?.Count() != shPath.Length
            && walk.WalkResponse?.Infos?.LastOrDefault()?.NodeType != NodeType.Command
            )
        {
            throw new InvalidOperationException($"Failed to find shell command in {string.Join("/", shPath)}");
        }
        var tag = walk.Tag;

        // let's open the command stream
        var openResponse = await ipcClient.Open(tag, logger);
        openResponse.ThrowIfError();

        // command expects to write to them, and then to read from them
        var writeResponse = await ipcClient.Write(tag, System.Text.Encoding.UTF8.GetBytes(""), logger);
        writeResponse.ThrowIfError();

        while (true)
        {
            string line = "";
            try
            {
                // let's read a line from the console 
                var readResponse = await ipcClient.Read(tag, logger);
                readResponse.ThrowIfError();
                Console.Write(readResponse.ReadResponse?.Data?.ToStringUtf8());

                line = Console.ReadLine();

                if (line == null || line.Trim().Length == 0)
                {
                    continue;
                }

                var terminalSendWrite = await ipcClient.Write(tag, System.Text.Encoding.UTF8.GetBytes(line), logger);
                terminalSendWrite.ThrowIfError();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing command: {Command}", line);
            }
        }
    }
}
