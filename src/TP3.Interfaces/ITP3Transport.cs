using System;
using System.Threading.Tasks;

namespace TP3.Interfaces;

public interface ITP3Transport : IDisposable
{
    Task Start();
    void Stop();
}
