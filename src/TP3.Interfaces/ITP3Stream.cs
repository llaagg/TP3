namespace TP3.Interfaces
{
    public interface ITP3Stream
    {
        Task Close();

        Task WriteAsync(Memory<byte> data);

        Task<Memory<byte>> ReadAsync();
    }
}