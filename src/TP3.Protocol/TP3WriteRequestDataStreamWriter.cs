using TP3.Interfaces;

namespace TP3.Messages;

public class TP3WriteRequestDataStreamWriter
{
    public TP3WriteRequestDataStreamWriter(ITP3DataStream stream)
    {
        
    }

    public async Task<ulong> Write(TP3WriteRequest request)
    {
        // Serialize the request and write it to the stream
        return 0;
    }
}