using Microsoft.Extensions.Logging;
using TP3.Messages;

public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

public class StatusChangedEventArgs
{
    public string Status { get; }

    public StatusChangedEventArgs(string status)
    {
        Status = status;
    }
}

public class TP3ClientWrapper
{
    private readonly TP3.Protocol.Client.TP3Client ipcClient;
    private readonly ILogger? logger;

    public string Status { get; private set; } = "Idle";

    public TP3ClientWrapper(ILogger? logger = null)
    {
        this.logger = logger;
        ipcClient = new TP3.Protocol.Client.TP3Client();
    }

    public async Task Connect()
    {
        try
        {
            this.UpdateStatus("Connecting", "Attempting to connect to IPC server...");
            await ipcClient.ConnectAsync();
            this.UpdateStatus("Connected", "Successfully connected to IPC server.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to connect to IPC server.");
            this.UpdateStatus("Disconnected", $"Failed to connect to IPC server: {ex.Message}");
        }
    }

    private void UpdateStatus(string state, string message)
    {
        logger?.LogInformation("Status changed: {State} - {Message}", state, message);
        StatusChanged?.Invoke(this, new StatusChangedEventArgs($"{state}: {message}"));
        this.Status = state;
    }

    public event StatusChangedEventHandler? StatusChanged;

    public async Task<IEnumerable<TP3.Protocol.TP3StatPayload>> ListenForResponses()
    {
        var newtag =  Guid.NewGuid().ToString("N").Substring(0, 8);

        var attachRequest = new TP3Message()
        {
            Tag = newtag,
            AttachRequest = new TP3AttachRequest()
        };
        var attachResponse = await this.ipcClient.SendAndWaitOne(attachRequest, logger).ConfigureAwait(false);
        if (attachResponse is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if (attachResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException($"Error received from IPC server: {attachResponse.Error?.Message}");
        }
        if (attachResponse.PayloadCase != TP3Message.PayloadOneofCase.AttachResponse)
        {
            throw new InvalidOperationException($"Unexpected response type: {attachResponse.PayloadCase}");
        }


        var openResponse = await ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = newtag,
            OpenRequest = new TP3OpenRequest()
        }, logger).ConfigureAwait(false);

        if(openResponse?.Tag is null)
        {
            throw new InvalidOperationException("Failed to open directory for listing.");
        }

        foreach (var item in ipcClient.ReadDirectory(openResponse, logger))
        {
            logger?.LogInformation("Received item: {Name} - {NodeType}", item.Name, item.Info.NodeType);
        }

        return null; // Placeholder, you can return the actual list of items if needed
    }


    public List<string> Sessions { get; } = new List<string>();
}
