using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Client;

public static class TP3ClientHelpers
{
    
    public static async Task<TP3Message?> SendAndWaitOne(this TP3Client ipcClient, TP3Message request, ILogger? logger = null)
    {
        logger?.LogInformation("Sending request to IPC server: {Request}", request);
        await ipcClient.SendMessageAsync(request);
        var response =  await ipcClient.ReceiveSingleResponse(logger);
        
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

    public static Stream GetStream(this TP3Client pipe, TP3Message openResponse, ILogger? logger = null)
    {
        IEnumerable<TP3ReadResponse> data = pipe.SynchronousDataProvider(openResponse.Tag, openResponse.OpenResponse.Iounit,  logger);
        TP3ReadResponseDataStream stream = new TP3ReadResponseDataStream(data);

        return stream;
    }

    public static IEnumerable<TP3StatPayload> ReadDirectory(this TP3Client pipe, TP3Message openResponse, ILogger? logger = null)
    {
        IEnumerable<TP3ReadResponse> data = pipe.SynchronousDataProvider(openResponse.Tag, openResponse.OpenResponse.Iounit,  logger);
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
            
            var message = Task.Run(async () =>
            {
                logger?.LogInformation("Sending read request to IPC server: {Request}", readRequest);
                var message = await ipcClient.SendAndWaitOne(readRequest, logger).ConfigureAwait(false);
                return message;
            }).Result;
            
            if(message is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if(message.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                logger?.LogError("Error received from IPC server: {Error}", message.Error?.Message);
                //throw new InvalidOperationException($"Error received from IPC server: {message.Error?.Message}");
                yield break;
            }

            var count = message.ReadResponse.Data.Count();

            if (count == 0 || count < maxBytes)
            {
                break;
            }
            offset += (ulong)count;

            yield return message!.ReadResponse;

        }
        yield break;
    }
}