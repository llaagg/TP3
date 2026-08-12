using Lopla.Language.Binary;
using Lopla.Language.Interfaces;
using Lopla.Language.Libraries;

namespace TP3.Shell;

public class TP3 : BaseLoplaLibrary
{
    public string Name => "TP3";

    public void Register(IRuntime runtime)
    {
        this.Add("exec", Exec, "path");
        this.Add("ls", LS);
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