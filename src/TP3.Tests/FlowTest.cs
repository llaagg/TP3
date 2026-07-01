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

    [Fact]
    public async Task Agent_IsAbleToReturnStreamOfData()
    {
        var logger = A.Fake<ILogger>();
        var agentHost = new AgentHost(port: 6000, ipcPort: 6001, logger: logger);

        await agentHost.Start();
        
        

        // Here you can add assertions to check if the agent is able to return a stream of data.
        // For example, you might want to send a request to the agent and verify that it returns the expected data.

        agentHost.Stop();
        agentHost.Dispose();
    }
}