using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace TP3.CLI;

public static partial class CLI
{
    private static Command ExecCommand(Argument<string> commndFile, Argument<string[]> argsArgument, Option<LogLevel> logLevel)
    {
        var execCommand = new Command("exec", "Execute a command in the IPC server using webdav mounted")
        {
            logLevel,
            commndFile,
            argsArgument,
        };

        execCommand.SetHandler(ExecuteCommand, commndFile, argsArgument, logLevel);

        return execCommand;
    }

    private static void ExecuteCommand(
        string commandFile,
        string[] args,
        LogLevel logLevel)
    {
        using var fs = File.Open(commandFile, FileMode.Open, FileAccess.ReadWrite);

        using var writer = new StreamWriter(fs, leaveOpen: true);
        writer.WriteLine(string.Join(" ", args));
        writer.Flush();

        fs.Position = 0;

        using var reader = new StreamReader(fs);
        Console.Write(reader.ReadToEnd());
        fs.Close();
    }
}