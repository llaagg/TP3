using System;
using System.Threading.Tasks;
using TP3.Messages;

namespace TP3.Interfaces;

/// <summary>
/// In one network transport, 
/// there can be multiple user sessions
/// in one user session could be multple logical (based on tags) sessions.
/// </summary>
public interface INetworkTransport : IDisposable
{
    Task Start();
    void Stop();
    Task Send(INetworkPipe session, TP3Message message);
    Task Init(ITP3Transport transport);
}
