using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.CloudFilter;

public class FoldersService : BaseDirectoryNode, IService
{
    public FoldersService()
        : base("folders")
    {
    }

    public void Dispose()
    {
    }

    public async Task Init(IAgent me)
    {
    }

    public async Task Start()
    {
    }

    public async Task Stop()
    {
    }
}
