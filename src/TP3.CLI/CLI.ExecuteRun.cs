using System.CommandLine;
using System.Text;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

namespace TP3.CLI;

public static partial class CLI
{
    private static Command RunCommand(
        Option<int> ipcPortOption,
        Option<int> bePatientAndWaitForServer,
        Argument<string> commandPath,
        Argument<string[]> commandArgs,
        Option<Microsoft.Extensions.Logging.LogLevel> logLevel)
    {
        var runCommand = new Command("run", "Run TP3 command node by path")
        {
            ipcPortOption,
            bePatientAndWaitForServer,
            logLevel,
            commandPath,
            commandArgs
        };

        runCommand.SetHandler(ExecuteRun, ipcPortOption, bePatientAndWaitForServer, commandPath, commandArgs, logLevel);
        return runCommand;
    }

    private static async Task ExecuteRun(int ipcPort, int waitForServer, string commandPath, string[] args, Microsoft.Extensions.Logging.LogLevel level)
    {
        var logger = InitilizeLogger(level);
        var ipcClient = new TP3Client(ipcPort, logger, waitForServer);

        await ipcClient.ConnectAsync().ConfigureAwait(false);
        try
        {
            var attachResponse = await ipcClient.Attach(logger).ConfigureAwait(false);
            attachResponse.ThrowIfError();
            var rootTag = attachResponse!.Tag;

            var segments = SplitPathSegments(commandPath);
            var walkResponse = await ipcClient.Walk(rootTag, segments, logger).ConfigureAwait(false);
            walkResponse.ThrowIfError();
            var runTag = walkResponse!.Tag;

            if(segments.Length !=  walkResponse.WalkResponse?.Infos?.Count)
            {
                logger.LogError("Command path not found. Please provide a valid command path.");
                return;
            }

            var openResponse = await ipcClient.Open(runTag, logger).ConfigureAwait(false);
            openResponse.ThrowIfError();

            var payload = args.Length == 0
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(string.Join(" ", args) + Environment.NewLine);

            var writeResponse = await ipcClient.Write(runTag, payload, logger).ConfigureAwait(false);
            writeResponse.ThrowIfError();

            await PrintReadData(ipcClient, runTag, logger).ConfigureAwait(false);
        }
        finally
        {
            await ipcClient.DisconnectAsync().ConfigureAwait(false);
        }
    }
}
