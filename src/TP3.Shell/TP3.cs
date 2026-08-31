using Lopla.Language.Binary;
using Lopla.Language.Errors;
using Lopla.Language.Interfaces;
using Lopla.Language.Libraries;
using TP3.Protocol.Client;

namespace TP3.Shell;

public class TP3 : BaseLoplaLibrary
{
    private TP3Client tp3client;

    public string Name => "TP3";

    public TP3(TP3Client tP3Client)
    {
        this.tp3client = tP3Client;
    }

    public void Register(IRuntime runtime)
    {
        this.Add("connect", Connect, "port");
        this.Add("walk", Walk);
        this.Add("ls", LS);
        this.Add("exec", Exec, "path");
    }

    private Result Walk(Mnemonic expression, IRuntime runtime)
    {
        throw new NotImplementedException();
    }

    private Result Connect(Mnemonic expression, IRuntime runtime)
    {
        Lopla.Language.Binary.String portObj = runtime.GetVariable("port")?.Get(runtime) as Lopla.Language.Binary.String;

        if (portObj != null && int.TryParse(portObj.Value, out int port))
        {
            tp3client.IpcPort = port;
            tp3client.ConnectAsync().Wait();
        }
        else
        {
            runtime.AddError(new RuntimeError("Type not supported " + (portObj != null ? portObj.GetType().Name : "null")));
        }

        return new Result();
    }

    private Result LS(Mnemonic expression, IRuntime runtime)
    {
        Console.WriteLine("Listing files in the current directory:");
        return new Result();
    }
    
    private Result Exec(Mnemonic expression, IRuntime runtime)
    {
        Console.WriteLine("Executing script from file: ");
        return new Result();
    }
}