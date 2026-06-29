using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TP3.CLI;

public static class IpcClient
{
    public static async Task<string> SendAsync(string host, int port, string message)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port).ConfigureAwait(false);

        using var networkStream = tcpClient.GetStream();
        using var writer = new StreamWriter(networkStream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(networkStream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(message).ConfigureAwait(false);
        var response = await reader.ReadLineAsync().ConfigureAwait(false);
        return response ?? string.Empty;
    }
}
