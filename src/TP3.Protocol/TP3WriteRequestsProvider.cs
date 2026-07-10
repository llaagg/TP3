using Google.Protobuf;
using TP3.Messages;

namespace TP3.Protocol;

public class TP3WriteRequestsProvider
{
    /// <summary>
    /// Converts a stream of data into a sequence of TP3WriteRequest messages, each containing a chunk of data and its corresponding offset.
    /// The size of each chunk is determined by the Iounit parameter.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="Iounit"></param>
    public TP3WriteRequestsProvider(Stream stream, ulong Iounit = 1000)
    {
        this.stream = stream;
        this.Iounit = Iounit;
    }

    private readonly Stream stream;
    private readonly ulong Iounit;

    public IEnumerable<TP3WriteRequest> GetWriteRequests()
    {
        var buffer = new byte[Iounit];
        int bytesRead;
        ulong offset = 0;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            var data = ByteString.CopyFrom(buffer, 0, bytesRead);
            yield return new TP3WriteRequest { Data = data, Offset = offset };
            offset += (ulong)bytesRead;
        }
    }
}