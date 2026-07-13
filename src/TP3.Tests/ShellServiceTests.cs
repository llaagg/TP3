using TP3.Interfaces;
using TP3.Service.Shell;

namespace TP3.Tests.Protocol;

public class ShellServiceTests
{
    [Fact]
    public async Task Shell_InReadStream_WaitsUntilWriterProvidesBytes()
    {
        var shell = new Shell("terminal-test");
        var buffer = new byte[1];

        var readTask = shell.GetInReadStream().ReadAsync(buffer, 0, 1);

        var completedBeforeWrite = await Task.WhenAny(readTask, Task.Delay(150));
        Assert.NotEqual(readTask, completedBeforeWrite);

        await shell.inStream.WriteAsync(new byte[] { 0x2A }, 0, 1);
        await shell.inStream.FlushAsync();

        var completedAfterWrite = await Task.WhenAny(readTask, Task.Delay(1000));
        Assert.Equal(readTask, completedAfterWrite);
        Assert.Equal(1, await readTask);
        Assert.Equal(0x2A, buffer[0]);
    }

    [Fact]
    public async Task Shell_OutStream_ReceivesBytesWrittenByServiceSide()
    {
        var shell = new Shell("terminal-test");
        var readBuffer = new byte[3];

        await shell.GetOutWriteStream().WriteAsync(new byte[] { (byte)'o', (byte)'k', (byte)'!' }, 0, 3);
        await shell.GetOutWriteStream().FlushAsync();

        var read = await shell.outStream.ReadAsync(readBuffer, 0, readBuffer.Length);

        Assert.Equal(3, read);
        Assert.Equal("ok!", System.Text.Encoding.UTF8.GetString(readBuffer));
    }

    [Fact]
    public async Task AddNewShell_ReturnsQuickly_AndAddsTerminalNodeToState()
    {
        var service = new ShellService();

        var addTask = service.AddNewShell();
        var completed = await Task.WhenAny(addTask, Task.Delay(500));

        Assert.Equal(addTask, completed);

        var terminalName = await addTask;
        Assert.StartsWith("terminal-", terminalName);

        var stateNode = service.Children!.Single(n => n.Name == "state");
        Assert.Contains(stateNode.Children!, n => n.Name == terminalName && n.NodeType == TP3.Messages.NodeType.Directory);
    }

    [Fact]
    public async Task AddNewShell_Listener_ForwardsInputBytesToOutput()
    {
        var service = new ShellService();
        await service.Start();

        var terminalName = await service.AddNewShell();

        var stateNode = service.Children!.Single(n => n.Name == "state");
        var terminalNode = stateNode.Children!
            .OfType<Shell>()
            .Single(n => n.Name == terminalName);

        var payload = new byte[] { 0x41, 0x42, 0x43 };
        var readBuffer = new byte[payload.Length];

        var readTask = terminalNode.outStream.ReadAsync(readBuffer, 0, readBuffer.Length);

        await terminalNode.inStream.WriteAsync(payload, 0, payload.Length);
        await terminalNode.inStream.FlushAsync();

        var completed = await Task.WhenAny(readTask, Task.Delay(1000));
        Assert.Equal(readTask, completed);

        var read = await readTask;
        Assert.Equal(payload.Length, read);
        Assert.Equal(payload, readBuffer);

        await service.Stop();
    }

    [Fact]
    public async Task Stop_CancelsShellListener_WithoutHanging()
    {
        var service = new ShellService();
        await service.Start();
        await service.AddNewShell();

        var stopTask = service.Stop();
        var completed = await Task.WhenAny(stopTask, Task.Delay(1000));

        Assert.Equal(stopTask, completed);
        await stopTask;
    }
}
