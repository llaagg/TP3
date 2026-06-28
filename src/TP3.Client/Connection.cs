using TP3.Interfaces;

namespace TP3.Client;

public class Connection : IConnection
{
    private readonly string connectionString;

    public Connection(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public void Connect()
    {
        var uri = new Uri(connectionString);
        var protocol = uri.Scheme;
        var host = uri.Host;        
    }
}
