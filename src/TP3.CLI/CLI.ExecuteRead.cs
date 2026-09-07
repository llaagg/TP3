using System.CommandLine;
using System.Text;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

namespace TP3.CLI;

public static partial class CLI
{
    private static Command ReadCommand(
        Option<int> ipcPortOption,
        Option<int> bePatientAndWaitForServer,
        Argument<string> singlePath,
        Option<LogLevel> logLevel)
    {
        var readCommand = new Command("read", "Read file or command output at TP3 path")
        {
            ipcPortOption,
            bePatientAndWaitForServer,
            logLevel,
            singlePath
        };

        readCommand.SetHandler(ExecuteRead, ipcPortOption, bePatientAndWaitForServer, singlePath, logLevel);
        return readCommand;
    }

    private static async Task ExecuteRead(int ipcPort, int waitForServer, string path, LogLevel level)
    {
        var logger = InitilizeLogger(level);
        
        var ipcClient = new TP3Client(ipcPort, logger, waitForServer);

        await ipcClient.ConnectAsync().ConfigureAwait(false);
        try
        {
            var attachResponse = await ipcClient.Attach(logger).ConfigureAwait(false);
            attachResponse.ThrowIfError();
            var rootTag = attachResponse!.Tag;

            var segments = SplitPathSegments(path);
            var walkResponse = await ipcClient.Walk(rootTag, segments, logger).ConfigureAwait(false);
            walkResponse.ThrowIfError();

            var effectiveTag = walkResponse!.Tag;
            var openResponse = await ipcClient.Open(effectiveTag, logger).ConfigureAwait(false);
            openResponse.ThrowIfError();

            var nodeType = openResponse!.OpenResponse?.Info?.NodeType;
            if (nodeType == NodeType.Directory)
            {
                foreach (var item in ipcClient.ReadDirectory(openResponse, logger))
                {
                    Console.WriteLine(item.Name);
                }

                return;
            }

            await PrintReadData(ipcClient, effectiveTag, logger).ConfigureAwait(false);
        }
        finally
        {
            await ipcClient.DisconnectAsync().ConfigureAwait(false);
        }
    }

    private static async Task PrintReadData(TP3Client ipcClient, string? tag, ITP3Logger logger)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new InvalidOperationException("Read tag is missing.");
        }

        const uint chunkSize = 64 * 1024;
        ulong offset = 0;

        while (true)
        {
            var response = await ipcClient.SendAndWaitOne(new TP3Message
            {
                Tag = tag,
                ReadRequest = new TP3ReadRequest
                {
                    Offset = offset,
                    MaxBytes = chunkSize
                }
            }, logger).ConfigureAwait(false);

            response.ThrowIfError();
            var data = response!.ReadResponse?.Data;
            var count = data?.Length ?? 0;
            if (count == 0)
            {
                break;
            }

            Console.Write(Encoding.UTF8.GetString(data!.ToByteArray()));

            if ((uint)count < chunkSize)
            {
                break;
            }

            offset += (ulong)count;
        }
    }
}
