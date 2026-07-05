using Microsoft.Extensions.Logging;
using TP3.Messages;

public class TP3Client
{
    private readonly TP3.Protocol.Client.TP3Client ipcClient;
    private readonly ILogger? logger;

    public TP3Client(ILogger? logger = null)
    {
        this.logger = logger;
        ipcClient = new TP3.Protocol.Client.TP3Client();
    }

    public async Task Connect()
    {
        await ipcClient.ConnectAsync();
    }

    public async Task<IEnumerable<TP3.Protocol.TP3StatPayload>> ListenForResponses()
    {
        var attachRequest = new TP3Message()
        {
            Tag = Guid.NewGuid().ToString("N").Substring(0, 8),
            AttachRequest = new TP3AttachRequest()
        };
        var attachResponse = await this.ipcClient.SendAndWaitOne(attachRequest, logger).ConfigureAwait(false);
        if(attachResponse is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if(attachResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException($"Error received from IPC server: {attachResponse.Error?.Message}");
        }

        return null;
    }

}
