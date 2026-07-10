using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Protocol;

public class TP3WriteRequestDataStreamWriter
{
    public TP3WriteRequestDataStreamWriter(ITP3DataStream stream)
    {
        this.stream = stream;
    }

    private readonly ITP3DataStream stream;

    public async Task<ulong> Write(TP3WriteRequest request)
    {
        if (request.Data == null)
        {
            throw new ArgumentNullException(nameof(request.Data), "Data cannot be null.");
        }
        var written = await stream.Write(request.Offset, request.Data.ToByteArray());
        
        if(written != (ulong)request.Data.Length)
        {
            throw new InvalidOperationException($"Expected to write {request.Data.Length} bytes, but only wrote {written} bytes.");
        }
        return written;
    }
}
