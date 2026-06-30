namespace TP3.CLI;

public class CliClient
{
    private static bool isRunning = true;

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
        { "HI", async (ipcClient, message) => {
            var response = await ipcClient.SendAsync($"LIST {message}");
            Console.WriteLine(response);
        }},
        { "HELP", async (ipcClient, message) => {
            Console.WriteLine("Available local commands:");
            Console.WriteLine("HELP - Show this help message");
            Console.WriteLine("HI <message> - Send a HI message to the local IPC server");
            Console.WriteLine("ECHO <message> - Send an ECHO message to the local IPC server");
            Console.WriteLine("QUIT - Exit the CLI");
        }},
        { "ECHO", async (ipcClient, message) => {
            var response = await ipcClient.SendAsync($"ECHO {message}");
            Console.WriteLine(response);
        }},
        { "QUIT", async (ipcClient, message) => {
            var response = await ipcClient.SendAsync($"QUIT");
            Console.WriteLine(response);
            isRunning = false;
        }}
    };
}