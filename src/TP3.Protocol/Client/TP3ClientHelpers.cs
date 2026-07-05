using Microsoft.Extensions.Logging;
using TP3.Messages;
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
}