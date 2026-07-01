using System;
using System.Threading.Tasks;
using TP3.Messages;

namespace TP3.Interfaces;

public interface INetworkTransport : IDisposable
{
    Task Start();
    void Stop();
    Task PublishEventAsync(string eventText);
    Task Send(TP3Message message);
}
