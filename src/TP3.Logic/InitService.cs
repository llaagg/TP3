using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Base;

public class InitService : BaseDirectoryNode, IService
{
    public InitService() : base("init-service")
    {
        this.AddChild(new BaseDirectoryNode("control", children: new INode[]{ new AutoStart()}));
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

internal class AutoStart : BaseControlParamsArgsCommand
{
    public AutoStart() 
    {
    }

    protected override async Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Starting.\n"));
        
        Thread.Sleep(10000);
        await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Done.\n"));
    }
}