using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using TP3.Messages;
using TP3.Protocol.Client;

namespace TP3.CLI;

public static partial class CLI
{
    public static ILogger InitilizeLogger(LogLevel logLevel)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                })
                .SetMinimumLevel(logLevel);
        });

        var logger = loggerFactory.CreateLogger(Assembly.GetExecutingAssembly().GetName().Name!);
        return logger;
    }
    
    public static async Task<TP3Message?> Attach(this TP3Client ipcClient, ILogger logger)
    {
        var attachRequest = new TP3Message()
        {
            Tag = Guid.NewGuid().ToString("N").Substring(0, 8),
            AttachRequest = new TP3AttachRequest()
        };
        var attachResponse = await ipcClient.SendAndWaitOne(attachRequest, logger).ConfigureAwait(false);

        return attachResponse;
    }
    public static async Task<TP3Message?> Walk(this TP3Client ipcClient, string? tag, string[] path, ILogger logger)
    {
        var walkRequest = new TP3Message()
        {
            Tag = tag ,
            WalkRequest = new TP3WalkRequest()
            {
                NewTag = Guid.NewGuid().ToString("N").Substring(0, 8),
                Path = { path }
            }
        };
        var walkResponse = await ipcClient.SendAndWaitOne(walkRequest, logger).ConfigureAwait(false);

        return walkResponse;
    }
    
    public static async Task<TP3Message?> Write(this TP3Client ipcClient, string? tag, byte[] data, ILogger logger)
    {
        var writeRequest = new TP3Message()
        {
            Tag = tag ,
            WriteRequest = new TP3WriteRequest()
            {
                Data =  Google.Protobuf.ByteString.CopyFrom(data)
            }
        };
        var writeResponse = await ipcClient.SendAndWaitOne(writeRequest, logger).ConfigureAwait(false);

        return writeResponse;
    }

    public static async Task<TP3Message?> Open(this TP3Client ipcClient, string? tag, ILogger logger)
    {
        var openRequest = new TP3Message()
        {
            Tag = tag ,
            OpenRequest = new TP3OpenRequest()         
        };
        var openResponse = await ipcClient.SendAndWaitOne(openRequest, logger).ConfigureAwait(false);

        return openResponse;
    }


    public static async Task<TP3Message?> Read(this TP3Client ipcClient, string? tag, ILogger logger)
    {
        var readRequest = new TP3Message()
        {
            Tag = tag,
            ReadRequest = new TP3ReadRequest()
        };
        var readResponse = await ipcClient.SendAndWaitOne(readRequest, logger).ConfigureAwait(false);

        if (readResponse is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if (readResponse.Error is not null)
        {
            throw new InvalidOperationException($"Error received from IPC server: {readResponse.Error?.Message}");
        }

        return readResponse;
    }

    public static void ThrowIfError(this TP3Message? message)
    {
        if (message is null)
        {
            throw new InvalidOperationException("Received null response from IPC server.");
        }
        if (message.Error is not null)
        {
            throw new InvalidOperationException($"Error received from IPC server: {message.Error?.Message} {message.Error?.Args}");
        }
    }
}
