using TP3.Protocol;

namespace TP3.Service.Remote;

internal class Attach : BaseControlParamsArgsCommand
{
    private RemotesService service;

    public Attach(RemotesService service)
    {
        this.service = service;
    }

    protected override async Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        using var writer = new StreamWriter(output, leaveOpen: true)
        {
            AutoFlush = true
        };

        if(args is null || args.Length == 0)
        {
            Help(writer);
            return;
        }

        var protocol = args[0];
        // handle protocol and show help if bad
        if(protocol == "tcp")
        {
            await HandleTcpAttach(writer, args);
        }else
        {
            // nice help screen
            Help(writer);
        }
        return;
    }

    private static void Help(StreamWriter writer)
    {
        writer.WriteLine("Usage: attach <protocol> [args]");
        writer.WriteLine("Supported protocols:");
        // show default and description be like help
        writer.WriteLine("  tcp <host> <port> - attach to a remote TP3 server over TCP");
        // port is optional, default is 
        writer.WriteLine($"  port is optional, default is {TP3Consts.DefaultServerPort}");
        writer.WriteLine("   ex.: tcp localhost {}");
    }

    private async Task HandleTcpAttach(StreamWriter writer, string[] args)
    {
        var host = args.Length > 1 ? args[1] : "localhost";
        var port = args.Length > 2 ? int.Parse(args[2]) : TP3Consts.DefaultServerPort;

        var result = await this.service.AttachTcpRemote(host, port);

        if(result.Success)
        {
            writer.WriteLine($"OK");
        }
        else
        {
            writer.WriteLine($"NOK: {result.Message}");
        }
    }
}
