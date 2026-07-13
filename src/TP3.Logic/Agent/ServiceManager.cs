using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Agent;

internal class ServiceManager
{
    private IAgent agent;
    private ILogger? logger;
    private List<IService> services;

    public ServiceManager(IAgent agent, IEnumerable<IService> services, ILogger? logger = null)
    {
        this.agent = agent;
        this.logger = logger;
        this.services = services.ToList();
    }

    public async Task Init()
    {
        foreach (var service in services)
        {
            logger?.LogInformation("Initializing service: {ServiceName}", service.GetType().Name);
            try
            {
                await service.Init(agent);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to initialize service: {ServiceName}", service.GetType().Name);
            }
        }
    }

    public IList<IService> GetServices()
    {
        return this.services.ToList();
    }

    public async Task Start()
    {
        foreach (var service in this.services)
        {
            this.logger?.LogInformation("Starting service: {ServiceName}", service.GetType().Name);
            await service.Start();
        }
    }

    public async Task Stop()
    {
        var stopTasks = this.services.Select(s => s.Stop()).ToList();
        await Task.WhenAll(stopTasks);
    }
}