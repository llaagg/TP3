using System;
using System.Threading.Tasks;
using TP3.Messages;

namespace TP3.Interfaces;

public interface ITP3Transport : IDisposable
{
    Task Send(TP3Message message);
    Task Start();
    void Stop();
    Task Init(IRouter router);
}
