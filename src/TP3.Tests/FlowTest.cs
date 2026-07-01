using FakeItEasy;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;

public class FlowTest
{
    

    [Fact]
    public async Task AgentHost_StartsAndStopsSuccessfully()
    {
        var logger = A.Fake<ILogger>();
        var agentHost = new AgentHost(port: 6000, ipcPort: 6001, logger: logger);

        await agentHost.Start();

        // Here you can add assertions to check if the services are running or if the transports are active.
        // For example, you might want to check if the TCP and IPC transports are listening on the specified ports.

        agentHost.Stop();
        agentHost.Dispose();
    }
}