using System.Threading.Tasks;

namespace TP3.Interfaces;

public interface IAgentClient
{
    Task<bool> ConnectAsync();
}
