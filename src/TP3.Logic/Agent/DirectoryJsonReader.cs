using System.Text.Json;
using TP3.Interfaces;

namespace TP3.Service.FileSystem;

internal sealed class DirectoryJsonReader : Reader
{
    private readonly byte[][] records;
    private IEnumerable<INode>? data;

    public DirectoryJsonReader(INode directory)
    {
        if (string.IsNullOrWhiteSpace(directory.Qid))
        {
            records = Array.Empty<byte[]>();
            return;
        }

        // var entries = Directory.GetFileSystemEntries(absolutePath)
        //     .Select(p => new DirectoryEntryDto(
        //         Path.GetFileName(p) ?? p,
        //         Directory.Exists(p) ? "directory" : "file"));

        // var childrens = directory.Children ?? Array.Empty<INode>();

        // records = entries
        //     .Select(e => JsonSerializer.SerializeToUtf8Bytes(e))
        //     .ToArray();
        this.data = directory.Children;
    }

    public override Task<ServiceReadResult> ReadAsync(long offset, int maxBytes)
    {
        // var index = offset < 0 ? 0 : offset;

        // var bytes = data?.Select(d => JsonSerializer.SerializeToUtf8Bytes(new DirectoryEntryDto(d.Name, d.NodeType.ToString().ToLowerInvariant()))).ToArray();

        // if (data is null || index >= data.Count())
        // {
        //     return Task.FromResult(new ServiceReadResult
        //     {
        //         Data = Array.Empty<byte>(),
        //         NextOffset = index,
        //         IsEof = true
        //     });
        // }

        // var record = bytes?[index] ?? Array.Empty<byte>();
        // return Task.FromResult(new ServiceReadResult
        // {
        //     Data = record,
        //     NextOffset = index + 1,
        //     IsEof = false
        // });
        return new Task<ServiceReadResult>(() =>
        {
            var index = offset < 0 ? 0 : offset;

            var bytes = data?.Select(d => JsonSerializer.SerializeToUtf8Bytes(new DirectoryEntryDto(d.Name, d.NodeType.ToString().ToLowerInvariant()))).ToArray();

            if (data is null || index >= data.Count())
            {
                return new ServiceReadResult
                {
                    Data = Array.Empty<byte>(),
                    NextOffset = index,
                    IsEof = true
                };
            }

            var record = bytes?[index] ?? Array.Empty<byte>();
            return new ServiceReadResult
            {
                Data = record,
                NextOffset = index + 1,
                IsEof = false
            };
        });
    }
}


