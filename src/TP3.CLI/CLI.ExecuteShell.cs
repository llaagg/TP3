using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;
using System.Text;

namespace TP3.CLI;

public static partial class CLI
{

    private static async Task ExecuteShell(int ipcPort, int bePatientAndWaitForServer, Microsoft.Extensions.Logging.LogLevel level)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // bump it - debug
        //level = LogLevel.Debug;
        var logger = InitilizeLogger(level);
        logger.LogInformation("Starting terminal session with IPC server on port {IpcPort}", ipcPort);

        // attaching to the IPC server
        var ipcClient = new TP3Client(ipcPort, logger, bePatientAndWaitForServer);
        await ipcClient.ConnectAsync();

        var attachResponse = await ipcClient.Attach(logger);
        attachResponse.ThrowIfError();
        var rootTag = attachResponse?.Tag;
        if (string.IsNullOrWhiteSpace(rootTag))
        {
            throw new InvalidOperationException("Attach response did not provide a root tag.");
        }

        // we are connected with tag
        logger.LogInformation("Connected to IPC server with tag {Tag}", rootTag);

        // var createPath = new[] { "services", "shell", "control", "create" };
        // var createWalk = await ipcClient.Walk(rootTag, createPath, logger);
        // createWalk.ThrowIfError();
        // if (createWalk?.WalkResponse?.Infos?.Count != createPath.Length
        //     || createWalk.WalkResponse.Infos.LastOrDefault()?.NodeType != NodeType.Command)
        // {
        //     throw new InvalidOperationException($"Failed to find create command in {string.Join("/", createPath)}");
        // }

        // var createTag = createWalk.Tag;
        // var createOpen = await ipcClient.Open(createTag, logger);
        // createOpen.ThrowIfError();

        // // Execute create command, terminal name is written to command output.
        // var createWrite = await ipcClient.Write(createTag, Array.Empty<byte>(), logger);
        // createWrite.ThrowIfError();

        // var createRead = await ipcClient.Read(createTag, logger);
        // createRead.ThrowIfError();
        // var terminalName = createRead.ReadResponse?.Data?.ToStringUtf8()?.Trim();
        // if (string.IsNullOrWhiteSpace(terminalName))
        // {
        //     throw new InvalidOperationException("Create command did not return terminal name.");
        // }

        // logger.LogInformation("Created terminal {TerminalName}", terminalName);

        // var inPath = new[] { "services", "shell", "state", terminalName, "in" };
        // var outPath = new[] { "services", "shell", "state", terminalName, "out" };

        // var inWalk = await ipcClient.Walk(rootTag, inPath, logger);
        // inWalk.ThrowIfError();
        // if (inWalk?.WalkResponse?.Infos?.Count != inPath.Length
        //     || inWalk.WalkResponse.Infos.LastOrDefault()?.NodeType != NodeType.File)
        // {
        //     throw new InvalidOperationException($"Failed to find input stream in {string.Join("/", inPath)}");
        // }

        // var outWalk = await ipcClient.Walk(rootTag, outPath, logger);
        // outWalk.ThrowIfError();
        // if (outWalk?.WalkResponse?.Infos?.Count != outPath.Length
        //     || outWalk.WalkResponse.Infos.LastOrDefault()?.NodeType != NodeType.File)
        // {
        //     throw new InvalidOperationException($"Failed to find output stream in {string.Join("/", outPath)}");
        // }

        // var inTag = inWalk.Tag;
        // var outTag = outWalk.Tag;

        // var inOpen = await ipcClient.Open(inTag, logger);
        // inOpen.ThrowIfError();
        // var outOpen = await ipcClient.Open(outTag, logger);
        // outOpen.ThrowIfError();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += OnCancel;

        async void OnCancel(object? sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            cts.Cancel();
            await Task.Yield();
        }

        // var readTask = Task.Run(async () =>
        // {
        //     while (!cts.Token.IsCancellationRequested)
        //     {
        //         TP3Message? readResponse;
        //         try
        //         {
        //             readResponse = await ipcClient.Read(outTag, logger);
        //         }
        //         catch
        //         {
        //             if (cts.Token.IsCancellationRequested)
        //             {
        //                 break;
        //             }

        //             throw;
        //         }

        //         readResponse.ThrowIfError();
        //         var chunk = readResponse?.ReadResponse?.Data?.ToStringUtf8();
        //         if (!string.IsNullOrEmpty(chunk))
        //         {
        //             Console.Write(chunk);
        //         }
        //     }
        // }, cts.Token);

        // try
        // {
        //     while (!cts.Token.IsCancellationRequested)
        //     {
        //         var line = Console.ReadLine();

        //         if (line is null)
        //         {
        //             cts.Cancel();
        //             break;
        //         }

        //         if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        //         {
        //             cts.Cancel();
        //             break;
        //         }

        //         if (line.Trim().Length == 0)
        //         {
        //             continue;
        //         }

        //         var bytes = Encoding.UTF8.GetBytes(line + Environment.NewLine);
        //         var inWrite = await ipcClient.Write(inTag, bytes, logger);
        //         inWrite.ThrowIfError();
        //     }
        // }
        // finally
        // {
        //     cts.Cancel();
        //     try
        //     {
        //         await readTask;
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         // expected on shutdown
        //     }

        //     Console.CancelKeyPress -= OnCancel;

        //     // Try to close working tags.
        //     if (!string.IsNullOrWhiteSpace(inTag))
        //     {
        //         await ipcClient.SendAndWaitOne(new TP3Message { Tag = inTag, ClunkRequest = new TP3ClunkRequest() }, logger);
        //     }
        //     if (!string.IsNullOrWhiteSpace(outTag))
        //     {
        //         await ipcClient.SendAndWaitOne(new TP3Message { Tag = outTag, ClunkRequest = new TP3ClunkRequest() }, logger);
        //     }
        //     if (!string.IsNullOrWhiteSpace(createTag))
        //     {
        //         await ipcClient.SendAndWaitOne(new TP3Message { Tag = createTag, ClunkRequest = new TP3ClunkRequest() }, logger);
        //     }
        // }
    }
}
