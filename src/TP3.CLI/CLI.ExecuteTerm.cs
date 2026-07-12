using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

namespace TP3.CLI;

public static partial class CLI
{

    private static async Task ExecuteTerm(int ipcPort, int bePatientAndWaitForServer, LogLevel level)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // bump it - debug
        //level = LogLevel.Debug;
        var logger = InitilizeLogger(level);
        logger.LogInformation("Starting terminal session with IPC server on port {IpcPort}", ipcPort);

        // attaching to the IPC server
        var client = new TP3Client(ipcPort, logger);
        var ipcClient = new TP3Client(ipcPort, logger, bePatientAndWaitForServer);
        await ipcClient.ConnectAsync();
        var attachResponse = await ipcClient.Attach(logger).ConfigureAwait(false);
        attachResponse.ThrowIfError();
        var tag = attachResponse?.Tag;
        

        // we are connected with tag
        logger.LogInformation("Connected to IPC server with tag {Tag}", tag);

        Context context = new Context()
        {
            RootTag = tag,
            ipcClient = ipcClient,
            logger = logger,
            nfo = attachResponse?.AttachResponse.Info
        };
    

        while (true)
        {
            Console.Write($"{context.path}:> ");
            // whatwever user will put let's send it to the IPC server
            // this should be just a path to walk and run Read.

            // depends whhat we discover afer walk
            // we either list the folder or we just just run command or with show the file content

            var line = Console.ReadLine();

            if (line == null || line.Trim().Length == 0)
            {
                continue;
            }

            try
            {
                if (line.StartsWith("walk"))
                {
                    var w = new Walk();
                    context = await w.Do(context, line);
                }else if (line.StartsWith("ls"))
                {
                    var l = new List();
                    await l.Do(context, line);
                }
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while processing command: {Command}", line);
            }


        }
    }
}

internal class List
{
    public List()
    {
    }

    public async Task Do(Context context, string line)
    {
        line = line.Substring("ls".Length).Trim();
        // Implement the list logic here
        if(context.nfo == null)
        {
            context.logger.LogError("No node info available. Please walk to a valid path first.");
            return;
        }
        if(context.nfo.NodeType != NodeType.Directory)
        {
            context.logger.LogError("Current node is not a directory. Please walk to a valid directory first.");
            return;
        }

        // i'll do the walk for new tag and clunk it
        var walkResponse = await context.ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = context.Tag ?? context.RootTag,
            WalkRequest = new TP3WalkRequest()
            {
                NewTag = Guid.NewGuid().ToString("N").Substring(0, 8)
            }
        });
        walkResponse.ThrowIfError();

        var openResponse = await context.ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = walkResponse!.Tag,
            OpenRequest = new TP3OpenRequest()
        }, context.logger);

        openResponse.ThrowIfError();


        foreach (var item in context.ipcClient.ReadDirectory(openResponse!, context.logger))
        {
            var empote = item.Info.NodeType == NodeType.Directory ? "📁" : "📄";
            Console.WriteLine($"{empote} {item.Name}");
        }

        await context.ipcClient.SendAndWaitOne(new TP3Message()
        {
            Tag = walkResponse!.Tag,
            ClunkRequest = new TP3ClunkRequest()
        }, context.logger);
    }
}

internal class Read
{
    public Read()
    {
    }

    public async Task Do(Context context, string line)
    {
        line = line.Substring("read ".Length);
        // Implement the read logic here
        if(context.nfo == null)
        {
            context.logger.LogError("No node info available. Please walk to a valid path first.");
            return;
        }
    }
}