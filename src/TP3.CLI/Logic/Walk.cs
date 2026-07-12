using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

public class Context
{
    public string RootTag;
    public string Tag;
    public string path;

    public TP3Client ipcClient;
    public ILogger logger;
    internal NodeInfo? nfo;
}

public class Walk
{
    public Walk()
    {
    }

    public async Task<Context> Do(Context context, string line)
    {
        line = line.Substring("walk ".Length);

        var walkRequest = new TP3WalkRequest();
        
        var path = new List<string>();
        // exctract path from the 
        if (line != null && line.Trim().Length > 0)
        {
            // split by /
            path.AddRange(line.Split('/').Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));
            walkRequest.Path.Add(path.ToArray());
        }

        // new tag, short guid
        var newTag = Guid.NewGuid().ToString("N").Substring(0, 8);
        walkRequest.NewTag = newTag;

        var command = new TP3Message()
        {
            Tag = context.Tag ?? context.RootTag,
            WalkRequest = walkRequest
        };
        // send it if number of returned path is smaller then provided
        // show a warning 
        var response = await context.ipcClient.SendAndWaitOne(command, context.logger);

        // check error
        if (response == null)
        {
            context.logger.LogError("No response from IPC server for command: {Command}", line);
            return context;
        }
        else if (response.PayloadCase == TP3Message.PayloadOneofCase.Error)
        {
            context.logger.LogError("Error from IPC server: {Error}", response.Error?.Message);
            return context;
        }

        if (response.PayloadCase == TP3Message.PayloadOneofCase.WalkResponse)
        {
            var walkResponse = response.WalkResponse;
            if (walkResponse == null)
            {
                context.logger.LogError("Walk response is null for command: {Command}", line);
                return context;
            }

            // check if path is long as number of nodes returned
            if (walkResponse.Infos.Count != path.Count)
            {
                context.logger.LogWarning("Walk response path count {Count} is different from requested path count {RequestedCount} for command: {Command}", walkResponse.Infos.Count, path.Count, line);
            }

            var numberOfNodes = walkResponse.Infos.Count;

            var currentpath = "/" + string.Join("/", path.Take(numberOfNodes));

            context.path = currentpath;
            context.Tag = newTag;
            context.nfo = walkResponse.Infos.LastOrDefault();

            return context;
        }
        return context;
    }
}