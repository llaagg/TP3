using TP3.Messages;

namespace TP3.CLI;

public class CliClient
{
    public static bool isRunning = true;

    public async Task CliInternalClient(IpcClient ipcClient)
    {
        while (isRunning)
        {
            Console.Write("# ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }
            var parts = input.Split(' ', 2);
            var command = parts[0].ToUpperInvariant();
            var message = parts.Length > 1 ? parts[1] : string.Empty;

            if (commandHandlers.TryGetValue(command, out var handler))
            {
                await handler(ipcClient, message);
            }
            else
            {
                Console.WriteLine("Unknown command: {0}", command);
            }
        }
    }

    private static readonly Dictionary<string, Func<IpcClient, string, Task>> commandHandlers = new()
    {
        {
            "READ", async (ipcClient, message) => {
                var response = await ipcClient.SendAsync(new TP3Message
                {
                    Command = TP3Command.READ,
                    Path = message.Split('/').ToList()
                });
                Console.WriteLine(response);
            }
        },
        {
            "HELP", async (ipcClient, message) => {
                Console.WriteLine("Available local commands:");
                Console.WriteLine("HELP - Show this help message");
                Console.WriteLine("READ <path> - Read a file from the local filesystem");
                Console.WriteLine("ECHO <message> - Send an ECHO message to the local IPC server");
                Console.WriteLine("QUIT - Exit the CLI");
            }
        }
    };
}