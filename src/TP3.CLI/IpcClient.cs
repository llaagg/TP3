using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TP3.Agent.Logic.Protocol;
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
        var payload = TP3Serializer.SerializeBytes(message);
        await networkStream.WriteAsync(payload.AsMemory(0, payload.Length)).ConfigureAwait(false);

        var response = await TP3Serializer.ReadMessageAsync(networkStream, CancellationToken.None).ConfigureAwait(false);
        return response.ToString();
    }
}
