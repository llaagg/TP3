using System;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Client;

public class Connection : IConnection, IDisposable
{
    private readonly string connectionString;
    private TcpClient? tcpClient;
    private readonly ILogger<Connection>? logger;

    public Connection(string connectionString, ILogger<Connection>? logger = null)
    {
        this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        this.logger = logger;
    }

    public void Connect()
    {
        var (host, port) = ParseTcpEndpoint(connectionString);
        logger?.LogInformation("Connecting to TCP agent at {Host}:{Port}", host, port);

        tcpClient = new TcpClient();
        tcpClient.Connect(host, port);

        logger?.LogInformation("Connected to TCP agent.");
    }

    public bool IsConnected => tcpClient?.Connected == true;

    public Stream? GetStream() => tcpClient?.GetStream();

    public void Dispose()
    {
        logger?.LogInformation("Disposing TCP client connection.");
        tcpClient?.Close();
        tcpClient?.Dispose();
        tcpClient = null;
    }

    private static (string Host, int Port) ParseTcpEndpoint(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && uri.Scheme is "tcp" or "tcp4" or "tcp6")
        {
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5000;
            return (host, port);
        }

        if (Uri.TryCreate($"tcp://{connectionString}", UriKind.Absolute, out uri))
        {
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5000;
            return (host, port);
        }

        throw new ArgumentException($"Invalid TCP connection string: {connectionString}", nameof(connectionString));
    }
}
