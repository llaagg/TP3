using System.Net.Sockets;
using System.Text;
using TP3.Messages;

namespace TP3.CLI;

public class IpcClient
{
    private readonly int ipcPort;
    private readonly string host;

    public IpcClient(string host = "127.0.0.1", int ipcPort = 5001)
    {
        this.ipcPort = ipcPort;
        this.host = host;
    }

    public async Task<string> SendAsync(TP3Message message)
    {
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, ipcPort).ConfigureAwait(false);

        using var networkStream = tcpClient.GetStream();
        using var writer = new StreamWriter(networkStream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(networkStream, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(message.ToString()).ConfigureAwait(false);
        var response = await reader.ReadLineAsync().ConfigureAwait(false);
        return response ?? string.Empty;
    }
}
