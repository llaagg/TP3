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
        // get hostname and user name
        var hostname = System.Net.Dns.GetHostName();
        var username = Environment.UserName;

        try
        {
            this.UpdateStatus("Connecting", "Attempting to connect to IPC server...");
            await ipcClient.ConnectAsync();
            // let's attach and get the root of all
            this.UpdateStatus("Connected", "Successfully connected to IPC server.");
            await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = "tp3-" + username + "@" + hostname,
                AttachRequest = new TP3AttachRequest()
            }, logger).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to connect to IPC server.");
            this.UpdateStatus("Disconnected", $"Failed to connect to IPC server: {ex.Message}");
        }
    }

    public async Task<string> NewSession(params string[] path)
    {
        //random tag fynny sounds using mix of wowels and consonants
        string tag = GenerateRandomTag(8);

        var walkReqeust = new TP3WalkRequest()
        {
            NewTag = tag,
        };

        walkReqeust.Path.AddRange(path);

        await ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = tag,
            WalkRequest = walkReqeust
        }, logger);

        return tag;
    }

    private string GenerateRandomTag(int v)
    {
        var random = new Random();
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        return new string(Enumerable.Repeat(chars, v)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private void UpdateStatus(string state, string message)
    {
        logger?.LogInformation("Status changed: {State} - {Message}", state, message);
        StatusChanged?.Invoke(this, new StatusChangedEventArgs($"{state}: {message}"));
        this.Status = state;
    }

    public event StatusChangedEventHandler? StatusChanged;


    public async Task Open(string? tag = null)
    {
        // short gui cut for open and attach
        
        string newtag = string.IsNullOrWhiteSpace(tag) ?  Guid.NewGuid().ToString() : tag;

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
    }

    public async Task ListFolder(
            string tag,
            Func<TP3.Protocol.TP3StatPayload, Task> folderHandler)
    {
        var openResponse = await ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = tag,
            OpenRequest = new TP3OpenRequest()
        }, logger).ConfigureAwait(false);

        if(openResponse?.Tag is null)
        {
            throw new InvalidOperationException("Failed to open directory for listing.");
        }


        await Task.Run(async () =>
        {
            foreach (var response in ipcClient.ReadDirectory(openResponse, logger))
            {
                await folderHandler(response);
            }
        });
    }


    public List<string> Sessions { get; } = new List<string>();
}
