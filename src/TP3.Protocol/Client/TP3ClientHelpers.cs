using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Client;

public static class TP3ClientHelpers
{
    
    public static async Task<TP3Message?> SendAndWaitOne(this TP3Client ipcClient, TP3Message request, ILogger? logger = null)
    {
        logger?.LogInformation("Sending request to IPC server: {Request}", request);
        await ipcClient.SendMessageAsync(request).ConfigureAwait(false);
        var response =  await ipcClient.ReceiveSingleResponse(logger).ConfigureAwait(false);
        
        return response;
    }

    
    public static async Task<TP3.Messages.TP3Message?> ReceiveSingleResponse(this TP3Client ipcClient, ILogger? logger = null)
    {
        logger?.LogInformation("Listening for a single response from the IPC server...");
        await foreach (var response in ipcClient.ListenAsync())
        {
            return response;
        }

        return null;
    }

    public static IEnumerable<TP3StatPayload> ReadDirectory(this TP3Client pipe, TP3OpenResponse openResponse, ILogger? logger = null)
    {
        IEnumerable<TP3ReadResponse> data = pipe.SynchronousDataProvider(openResponse.Tag, openResponse.Iounit,  logger);
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

    private static IEnumerable<TP3ReadResponse> SynchronousDataProvider(this TP3Client ipcClient, string tag, uint maxBytes = 0,  ILogger? logger = null)
    {
        var offset = 0UL;
        while (true)
        {
            var readRequest = new TP3Message()
            {
                Tag = tag,
                ReadRequest = new TP3ReadRequest()
                {
                    Offset = offset,
                    MaxBytes = maxBytes
                }
            };
            
            var message = ipcClient.SendAndWaitOne(readRequest, logger).Result;
            if(message is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if(message.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException($"Error received from IPC server: {message.Error?.Message}");
            }

            yield return message!.ReadResponse;
            var count = message.ReadResponse.Data.Count();

            if (count == 0 || count < maxBytes)
            {
                break;
            }
            offset += (ulong)count;
        }
        yield break;
    }
}