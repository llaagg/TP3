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
        // bump it - debug
        level = LogLevel.Debug;
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

        string currentpath = "/";
        while(true)
        {
            string newTag = Guid.NewGuid().ToString("N").Substring(0, 8);
            Console.Write($"{currentpath} ({newTag})> ");
            // whatwever user will put let's send it to the IPC server
            // this should be just a path to walk and run Read.
            
            // depends whhat we discover afer walk
            // we either list the folder or we just just run command or with show the file content

            var line = Console.ReadLine();

            List<string> path = new List<string>();

            var walkRequest = new TP3WalkRequest();
            // exctract path from the 
            if(line != null && line.Trim().Length > 0)
            {
                // split by /
                path.AddRange(line.Split('/').Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));
                walkRequest.Path.Add(path.ToArray());
            }
            
            walkRequest.NewTag = newTag;
            var command = new TP3Message()
            {
                Tag = tag,
                WalkRequest = walkRequest
            }; 
            // send it if number of returned path is smaller then provided
            // show a warning 
            var response = await ipcClient.SendAndWaitOne(command, logger);

            // check error
            if(response == null)
            {
                logger.LogError("No response from IPC server for command: {Command}", line);
                continue; 
            }else if(response.PayloadCase == TP3Message.PayloadOneofCase.Error)
            {
                logger.LogError("Error from IPC server: {Error}", response.Error?.Message);
                continue;
            }

            if(response.PayloadCase == TP3Message.PayloadOneofCase.WalkResponse)
            {
                var walkResponse = response.WalkResponse;
                if(walkResponse == null)
                {
                    logger.LogError("Walk response is null for command: {Command}", line);
                    continue;
                }
                
                // check if path is long as number of nodes returned
                if(walkResponse.Infos.Count != path.Count)
                {
                    logger.LogWarning("Walk response path count {Count} is different from requested path count {RequestedCount} for command: {Command}", walkResponse.Infos.Count, path.Count, line);
                }
                
                var numberOfNodes = walkResponse.Infos.Count;

                currentpath = "/" + string.Join("/", path.Take(numberOfNodes));
            }

            

            // clunk my friend
            var clunkRequest = new TP3Message()
            {
                Tag = newTag,
                ClunkRequest = new TP3ClunkRequest()
            };

        }
    }
}
