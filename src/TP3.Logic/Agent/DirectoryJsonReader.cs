using System.Text.Json;
using TP3.Interfaces;

namespace TP3.Service.FileSystem;

internal sealed class DirectoryJsonReader : Reader
{
    private readonly byte[][] records;

    public DirectoryJsonReader(bool isRootState, string? absolutePath)
    {
        if (isRootState)
        {
            records = DriveInfo.GetDrives()
                .Select(d => new DirectoryEntryDto(d.Name, "directory"))
                .Select(e => JsonSerializer.SerializeToUtf8Bytes(e))
                .ToArray();
            return;
        }

        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            records = Array.Empty<byte[]>();
            return;
        }

        var entries = Directory.GetFileSystemEntries(absolutePath)
            .Select(p => new DirectoryEntryDto(
                Path.GetFileName(p) ?? p,
                Directory.Exists(p) ? "directory" : "file"));
        records = entries
            .Select(e => JsonSerializer.SerializeToUtf8Bytes(e))
            .ToArray();
    }

    public override Task<ServiceReadResult> ReadAsync(long offset, int maxBytes)
    {
        var index = offset < 0 ? 0 : offset;
        if (index >= records.Length)
        {
            return Task.FromResult(new ServiceReadResult
            {
                Data = Array.Empty<byte>(),
                NextOffset = index,
                IsEof = true
            });
        }

        var record = records[index];
        return Task.FromResult(new ServiceReadResult
        {
            Data = record,
            NextOffset = index + 1,
            IsEof = false
        });
    }
}


