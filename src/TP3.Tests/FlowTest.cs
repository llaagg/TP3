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

        
    }
}