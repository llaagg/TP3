namespace TP3.Protocol.Base;

public class RWStreamString : RWStream
{
    public RWStreamString(Func<Task<string>> readFunc, Func<string, Task> writeFunc)
    {
        this.readFunc = readFunc;
        this.writeFunc = writeFunc;
    }

    private Func<Task<string>> readFunc;
    private Func<string, Task> writeFunc;

    public override async Task OnWrite(byte[] data)
    {
        var incomingData = System.Text.Encoding.UTF8.GetString(data);
        await writeFunc(incomingData);
    }

    public override async Task<byte[]> OnRead(ulong offset, ulong maxCount)
    {
        if(offset != 0)
        {
            return new byte[0];
        }

        var result = await readFunc();
        this.Position = (ulong)result.Length;
        return System.Text.Encoding.UTF8.GetBytes(result);
    }
}