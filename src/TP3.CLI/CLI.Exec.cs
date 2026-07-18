using System.CommandLine;
using System.Text;
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
        var commandText = string.Join(" ", args) + Environment.NewLine;
        File.WriteAllText(commandFile, commandText, Encoding.UTF8);

        var responseText = File.ReadAllText(commandFile, Encoding.UTF8);
        Console.Write(responseText);
    }
}