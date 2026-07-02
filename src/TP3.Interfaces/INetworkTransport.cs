using System;
using System.Threading.Tasks;
using TP3.Messages;

namespace TP3.Interfaces;

public interface INetworkTransport : IDisposable
{
    Task Start();
    void Stop();
    Task Send(TP3Message message);
    Task Init(IRouter router, ITP3Transport transport);
}
