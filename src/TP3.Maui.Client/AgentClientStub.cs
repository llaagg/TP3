using System.Threading.Tasks;
using TP3.Interfaces;

namespace TP3.Maui.Client
{
    public class AgentClientStub : IAgentClient
    {
        public Task<bool> ConnectAsync()
        {
            // TODO: implement real connection (gRPC/HTTP) to TP3.Agent.Service
            return Task.FromResult(true);
        }
    }
}
