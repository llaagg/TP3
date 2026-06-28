using Microsoft.Extensions.Logging;

namespace TP3.Client;

public static class ConnectionHelper
{
    public static Connection CreateTcpConnection(string connectionString, ILogger<Connection>? logger = null)
    {
        return new Connection(connectionString, logger);
    }

    public static Connection CreateTcpConnection(string host, int port, ILogger<Connection>? logger = null)
    {
        return new Connection($"tcp://{host}:{port}", logger);
    }

    public static Connection ConnectTcp(string connectionString, ILogger<Connection>? logger = null)
    {
        var connection = CreateTcpConnection(connectionString, logger);
        connection.Connect();
        return connection;
    }

    public static Connection ConnectTcp(string host, int port, ILogger<Connection>? logger = null)
    {
        var connection = CreateTcpConnection(host, port, logger);
        connection.Connect();
        return connection;
    }
}
