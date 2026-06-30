using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TP3.CLI;

public class IpcClient
{
    private readonly int ipcPort;
    private readonly string host;
    private readonly ILogger logger;

    public IpcClient(string host = "127.0.0.1", int ipcPort = 5001)
    {
        this.ipcPort = ipcPort;
        this.host = host;
    }

    public async Task<string> SendAsync(string message)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, ipcPort).ConfigureAwait(false);

        using var networkStream = tcpClient.GetStream();
        using var writer = new StreamWriter(networkStream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(networkStream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(message).ConfigureAwait(false);
        var response = await reader.ReadLineAsync().ConfigureAwait(false);
        return response ?? string.Empty;
    }

    public static async Task  Main()
    {
        var ipcClient = new IpcClient("127.0.0.1", 5001);

        while (true)
        {
            Console.Write("# ");
            var input = Console.ReadLine();
            if (input == null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Exiting IPC client.");
                break;
            }
            else
            {
                var result = await ipcClient.SendAsync(input);
                Console.WriteLine("{0}", result);
            }
        }
    }
}
