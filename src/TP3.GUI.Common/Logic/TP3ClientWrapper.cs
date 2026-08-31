using System.Runtime.Versioning;
using System.Text;
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
    private readonly ITP3Logger? logger;
    private string rootTag;

    public string Status { get; private set; } = "Idle";

    public TP3ClientWrapper(ITP3Logger? logger = null)
    {
        this.logger = logger;

        ipcClient = new TP3.Protocol.Client.TP3Client();
        
        // get hostname and user name
        var hostname = Environment.MachineName;
        var username = Environment.UserName;

        this.rootTag = "tp3-" + username + "@" + hostname;
    }

    public async Task Connect()
    {
        try
        {
            this.UpdateStatus("Connecting", "Attempting to connect to IPC server...");
            await ipcClient.ConnectAsync();
            // let's attach and get the root of all
            this.UpdateStatus("Connected", "Successfully connected to IPC server.");
            this.UpdateStatus("Attaching", "Attaching to root...");
            var attachResult = await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = this.rootTag,
                AttachRequest = new TP3AttachRequest()
            }, logger).ConfigureAwait(false);

            if(attachResult is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if(attachResult.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException($"Error received from IPC server: {attachResult.Error?.Message}");
            }
            this.UpdateStatus("Attached", "Successfully attached to root.");
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to connect to IPC server.");
            this.UpdateStatus("Disconnected", $"Failed to connect to IPC server: {ex.Message}");
        }
    }

    public async Task<string> NewSessionAndWalk(params string[] path)
    {
        //random tag fynny sounds using mix of wowels and consonants
        string tag = GenerateRandomTag(8);

        var walkReqeust = new TP3WalkRequest()
        {
            NewTag = tag,
        };

        walkReqeust.Path.AddRange(path);

        var walkResponse = await ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = this.rootTag,
            WalkRequest = walkReqeust
        }, logger);
        
        if(walkResponse is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if(walkResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException($"Error received from IPC server: {walkResponse.Error?.Message}");
        }
        if(walkResponse.WalkResponse.Infos.Count != path.Length)
        {
            throw new InvalidOperationException($"Walk response count ({walkResponse.WalkResponse.Infos.Count}) does not match the provided path length ({path.Length}).");
        }
        return tag;
    }
    public async Task CloseSession(string tag)
    {

        var response = await ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = tag,
            ClunkRequest = new TP3ClunkRequest()
        }, logger);

        if(response is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if(response.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException($"Error received from IPC server: {response.Error?.Message}");
        }
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
        if(openResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            throw new InvalidOperationException($"Error received from IPC server: {openResponse.Error?.Message}");
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

    public Stream GetStream(string tag)
    {
        var openResponse = Task.Run(async () =>
        {
            var openResponse = await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = tag,
                OpenRequest = new TP3OpenRequest()
            }, logger).ConfigureAwait(false);

            if(openResponse is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if(openResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException($"Error received from IPC server: {openResponse.Error?.Message}");
            }

            return openResponse;
        }).Result;

        var stream = ipcClient.GetStream(openResponse, logger);
        return stream;
    }

    public async Task<ulong> WriteFileAsync(string[] path, string content)
    {
        var tag = await NewSessionAndWalk(path);

        try
        {
            var openResponse = await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = tag,
                OpenRequest = new TP3OpenRequest()
            }, logger).ConfigureAwait(false);

            if (openResponse is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if (openResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException($"Error received from IPC server: {openResponse.Error?.Message}");
            }

            var writeResponse = await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = tag,
                WriteRequest = new TP3WriteRequest
                {
                    Offset = 0,
                    Data = Google.Protobuf.ByteString.CopyFrom(Encoding.UTF8.GetBytes(content ?? string.Empty))
                }
            }, logger).ConfigureAwait(false);

            if (writeResponse is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if (writeResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException($"Error received from IPC server: {writeResponse.Error?.Message}");
            }
            if (writeResponse.PayloadCase != TP3Message.PayloadOneofCase.WriteResponse)
            {
                throw new InvalidOperationException($"Unexpected response type: {writeResponse.PayloadCase}");
            }

            return writeResponse.WriteResponse.Count;
        }
        finally
        {
            await CloseSession(tag);
        }
    }

    public async Task<(string Tag, string Output)> RunCommand(string[] path, string arguments)
    {        
        var tag = await NewSessionAndWalk(path);

        try
        {
            var openResponse = await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = tag,
                OpenRequest = new TP3OpenRequest()
            }, logger).ConfigureAwait(false);

            if (openResponse is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if (openResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException($"Error received from IPC server: {openResponse.Error?.Message}");
            }

            var writeResponse = await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = tag,
                WriteRequest = new TP3WriteRequest
                {
                    Offset = 0,
                    Data = Google.Protobuf.ByteString.CopyFrom(Encoding.UTF8.GetBytes(arguments ?? string.Empty))
                }
            }, logger).ConfigureAwait(false);

            if (writeResponse is null)
            {
                throw new InvalidOperationException("Received null response from IPC server.");
            }
            if (writeResponse.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                throw new InvalidOperationException($"Error received from IPC server: {writeResponse.Error?.Message}");
            }

            var stream = ipcClient.GetStream(openResponse, logger);
            using var reader = new StreamReader(stream);
            var output = await reader.ReadToEndAsync();

            return (tag, output);
        }
        finally
        {
            await CloseSession(tag);
        }
    }

    public bool Connected => Status == "Connected" || Status == "Attached";

    public async Task Disconnect()
    {
        if (Connected)
        {
            await ipcClient.SendAndWaitOne(new TP3Message()
            {
                Tag = this.rootTag,
                ClunkRequest = new TP3ClunkRequest()
            }, logger).ConfigureAwait(false);
            
            await ipcClient.DisconnectAsync();
            this.UpdateStatus("Disconnected", "Disconnected from IPC server.");
        }
    }
}
