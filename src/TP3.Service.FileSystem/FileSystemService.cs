using System.Text;
using System.Text.Json;
using TP3.Interfaces;

namespace TP3.Service.FileSystem;

/// <summary>
/// Service that allows access to filesystem, in tp3 space
/// </summary>
public class FileSystemService : IPathDataService
{
    public FileSystemService()
    {
        this.State = new StateNode("fs:state");
    }

    public INode State { get; private set; } = null!;

    public INode Control { get; private set; } = null!;

    public INode Events { get; private set; } = null!;

    public async Task Init(IAgent me)
    {
    }

    public bool CanHandlePath(IReadOnlyList<string> fullPath)
    {
        if (fullPath.Count == 0)
        {
            return true;
        }

        return string.Equals(fullPath[0], nameof(FileSystemService), StringComparison.OrdinalIgnoreCase);
    }

    public INode? ResolvePath(IReadOnlyList<string> fullPath)
    {
        return ResolveNodeByPath(fullPath);
    }

    public ServiceReader CreateReader(INode node)
    {
        if (node is FileSystemNode fsNode)
        {
            return fsNode.IsDirectory
                ? new DirectoryJsonReader(isRootState: false, fsNode.AbsolutePath)
                : new FileBinaryReader(fsNode.AbsolutePath);
        }

        return new DirectoryJsonReader(isRootState: true, absolutePath: null);
    }

    private INode? ResolveNodeByPath(IReadOnlyList<string> requestPath)
    {
        var segments = requestPath
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        if (segments.Count > 0 && string.Equals(segments[0], nameof(FileSystemService), StringComparison.OrdinalIgnoreCase))
        {
            segments.RemoveAt(0);
        }

        if (segments.Count > 0 && string.Equals(segments[0], "state", StringComparison.OrdinalIgnoreCase))
        {
            segments.RemoveAt(0);
        }

        if (segments.Count == 0)
        {
            return this.State;
        }

        INode current = this.State;
        foreach (var segment in segments)
        {
            if (current.Children is null)
            {
                return null;
            }

            var next = current.Children.FirstOrDefault(child => EqualsPathSegment(child.Name, segment));
            if (next is null)
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    private static bool EqualsPathSegment(string value, string segment)
    {
        return string.Equals(NormalizePathSegment(value), NormalizePathSegment(segment), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePathSegment(string segment)
    {
        return segment.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private sealed class FileBinaryReader : ServiceReader
    {
        private readonly string absolutePath;

        public FileBinaryReader(string absolutePath)
        {
            this.absolutePath = absolutePath;
        }

        public override async Task<ServiceReadResult> ReadAsync(long offset, int maxBytes)
        {
            await using var stream = File.OpenRead(absolutePath);
            if (offset >= stream.Length)
            {
                return new ServiceReadResult
                {
                    Data = Array.Empty<byte>(),
                    NextOffset = offset,
                    IsEof = true
                };
            }

            stream.Seek(offset, SeekOrigin.Begin);
            var buffer = new byte[maxBytes];
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
            if (bytesRead <= 0)
            {
                return new ServiceReadResult
                {
                    Data = Array.Empty<byte>(),
                    NextOffset = offset,
                    IsEof = true
                };
            }

            var payload = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, payload, 0, bytesRead);

            return new ServiceReadResult
            {
                Data = payload,
                NextOffset = offset + bytesRead,
                IsEof = false
            };
        }
    }

    private sealed class DirectoryJsonReader : ServiceReader
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

    private sealed record DirectoryEntryDto(string Name, string Type);
}

