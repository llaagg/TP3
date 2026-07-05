using System.CommandLine;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.CLI;

public static partial class CLI
{

    private static async Task ExecuteList(int ipcPort, int waitForServer, bool enableEmoted, LogLevel logLevel, string[]? path)
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
        var walkResponse1 = await ipcClient.SendAndWaitOne(logger, new TP3Message()
        {
            Tag = tag,
            WalkRequest = walkRequest
        }).ConfigureAwait(false);
        walkResponse1.ThrowIfError();

        var openResponse = await ipcClient.SendAndWaitOne(logger, new TP3Message()
        {
            Tag = tag,
            OpenRequest = new TP3OpenRequest()
        }).ConfigureAwait(false);
        openResponse.ThrowIfError();

        if(openResponse?.Tag is null)
        {
            throw new InvalidOperationException("Failed to open directory for listing.");
        }

        foreach (var item in ipcClient.TReadOnADirectory(openResponse.Tag, logger))
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
    public static async Task<TP3Message?> Attach(this TP3Client ipcClient, ILogger logger)
    {
        var attachRequest = new TP3Message()
        {
            Tag = Guid.NewGuid().ToString("N").Substring(0, 8),
            AttachRequest = new TP3AttachRequest()
        };
        var attachResponse = await SendAndWaitOne(ipcClient, logger, attachRequest).ConfigureAwait(false);

        return attachResponse;
    }

    private static async Task<TP3Message?> SendAndWaitOne(this TP3Client ipcClient, ILogger logger, TP3Message request)
    {
        logger.LogInformation("Sending request to IPC server: {Request}", request);
        await ipcClient.SendMessageAsync(request).ConfigureAwait(false);
        var response =  await ReceiveSingleResponse(ipcClient, logger);
        
        return response;
    }

    public static void ThrowIfError(this TP3Message? message)
    {
        if (message is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if (message.Error is not null)
        {
            throw new InvalidOperationException($"Error received from IPC server: {message.Error?.Message}");
        }
    }

    private static IEnumerable<TP3ReadResponse> SynchronousDataProvider(this TP3Client ipcClient, string tag, ILogger logger)
    {
        var offset = 0UL;
        var maxbytes = 10000U;
        while (true)
        {
            var readRequest = new TP3Message()
            {
                Tag = tag,
                ReadRequest = new TP3ReadRequest()
                {
                    Offset = offset,
                    MaxBytes = maxbytes
                }
            };
            
            var messsgae = ipcClient.SendAndWaitOne(logger, readRequest).Result;
            messsgae.ThrowIfError();

            yield return messsgae!.ReadResponse;
            var count = messsgae.ReadResponse.Data.Count();

            if (count == 0 || count < maxbytes)
            {
                break;
            }
            offset += (ulong)count;
        }
        yield break;
    }


    private static IEnumerable<TP3StatPayload> TReadOnADirectory(this TP3Client pipe, string tag, ILogger logger)
    {
        IEnumerable<TP3ReadResponse> data = pipe.SynchronousDataProvider(tag, logger);
        TP3ReadResponseDataStream stream = new TP3ReadResponseDataStream(data);

        // // diagnostic: read all data and deserialize to TP3StatPayload
        // StreamReader reader = new StreamReader(stream);
        // var ms = new MemoryStream();
        // stream.CopyTo(ms);
        // ms.Position = 0;

        // // what do we hwve there...
        // string json = new StreamReader(ms).ReadToEnd();
        // ms.Position = 0;

        var stats = TP3StatPayloadExtensions.Deserilize(stream);
        return stats;
    }

}
