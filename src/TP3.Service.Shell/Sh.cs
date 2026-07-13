using TP3.Protocol;
namespace TP3.Service.Shell;

internal class Sh : BaseControlCommand
{
    private ShellService shellService;

    public Sh()
    {
        this.shellService = null;
    }

    protected override async Task HandleStreamCommand(Stream input, Stream output)
    {
        // say hi
        // read whatever they are saying
        using var reader = new StreamReader(input, leaveOpen: true);
        using var writer = new StreamWriter(output, leaveOpen: true);
        var line = await reader.ReadLineAsync();

        // show prompt
        // let' show nice prompt moth with frames using ascci and info about tp3 (three plus 3 using empotes)
        var motd = @"
          ╭──────────────────────────────────────────────────────────╮
          │                                                          │
          │                     ░▒▓  🌳+3  ▓▒░                       │
          │                                                          │
          │                     *** TP3 Bash ***                     │
          │                                                          │
          ╰──────────────────────────────────────────────────────────╯

        ";
        await writer.WriteLineAsync(motd);
        await writer.FlushAsync();

        while (true)
        {
            await writer.WriteAsync("$ ");
            await writer.FlushAsync();

            // wait for some data comming in to the steam
            line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }            
        }
    }
}
