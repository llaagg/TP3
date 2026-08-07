using Lopla.Language.Interfaces;
using Lopla.Language.Processing;
using Lopla.Language.Providers;

namespace TP3.Shell;

public static class ScriptRunner
{
    public static void Main(string[] args)
    {
        var script = "a=1; b=2; c=a+b; print(c);";

        //var project = new ProjectFromFolder("TP3");
        var project = new MemoryScripts("tp3", new List<ILibrary>()
            {
            new Lopla.Libs.IO(),
            },
            new List<string>()
            {
                script
            }
        );

        var p = new Runner();
        var runtime = p.Run(project);


        if (runtime.HasErrors)
            Console.WriteLine(runtime.ToString());

    }
}
