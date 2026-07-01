using System;
using System.Threading.Tasks;

namespace TP3.Agent.Logic.Transport;

public interface INetworkTransport : IDisposable
{
    Task Start();
    void Stop();
    Task PublishEventAsync(string eventText);
}
