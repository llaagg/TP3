using Microsoft.Extensions.Logging;
using TP3.Interfaces;
using TP3.Messages;

public class TP3Transport : ITP3Transport
{
    private readonly ILogger logger;
    private IRouter? router = null!;
    private readonly INetworkTransport networkTransport;

    public TP3Transport(ILogger logger, INetworkTransport networkTransport)
    {
        this.logger = logger;
        this.networkTransport = networkTransport;
    }

    public async Task Send(TP3Message message)
    {
        // thorws stuff into the network channel
        await networkTransport.Send(message);
    }

    public async Task Start()
    {
        logger.LogInformation("TP3Transport started.");
        await networkTransport.Start();
    }

    public void Stop()
    {
        logger.LogInformation("TP3Transport stopped.");
        networkTransport.Stop();
    }

    public async Task Init(IRouter router)
    {
        this.router = router;
        await this.networkTransport.Init(router, this);
        logger.LogInformation("TP3Transport initialized with router.");
    }

    public void Dispose()
    {
        networkTransport.Dispose();
        logger.LogInformation("TP3Transport disposed.");
    }
}