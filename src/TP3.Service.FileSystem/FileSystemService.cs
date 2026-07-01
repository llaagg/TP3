using System.Text;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.FileSystem;

/// <summary>
/// Service that allows access to filesystem, in tp3 space
/// </summary>
public class FileSystemService : IPathDataService
{
    private const int ChunkSize = 16 * 1024;

    public FileSystemService()
    {
        this.State = new StateNode();
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

    public Task<TP3Message> WalkAsync(TP3Message request)
    {
        if (!TryResolvePath(request.Path, out var absolutePath, out var isRootState, out var error))
        {
            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Path = request.Path,
                Error = error
            });
        }

        if (isRootState)
        {
            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Path = request.Path,
                Qid = "fs:/",
                NodeType = "directory"
            });
        }

        if (Directory.Exists(absolutePath))
        {
            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Path = request.Path,
                Qid = absolutePath,
                NodeType = "directory"
            });
        }

        if (File.Exists(absolutePath))
        {
            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Path = request.Path,
                Qid = absolutePath,
                NodeType = "file"
            });
        }

        return Task.FromResult(new TP3Message
        {
            Command = TP3Command.WALK,
            Path = request.Path,
            Error = "NotFound"
        });
    }

    public async Task<IReadOnlyList<TP3Message>> ReadAsync(TP3Message request)
    {
        if (!TryResolvePath(request.Path, out var absolutePath, out var isRootState, out var error))
        {
            return new[]
            {
                new TP3Message
                {
                    Command = TP3Command.READ,
                    Path = request.Path,
                    Error = error
                }
            };
        }

        if (isRootState)
        {
            var rootListing = string.Join(Environment.NewLine, DriveInfo.GetDrives().Select(d => d.Name));
            var payload = Encoding.UTF8.GetBytes(rootListing);
            return BuildChunks(request, "fs:/", "directory", payload);
        }

        if (Directory.Exists(absolutePath))
        {
            var entries = Directory.GetFileSystemEntries(absolutePath)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToArray();
            var listing = entries.Length == 0 ? "(empty)" : string.Join(Environment.NewLine, entries!);
            var payload = Encoding.UTF8.GetBytes(listing);
            return BuildChunks(request, absolutePath, "directory", payload);
        }

        if (File.Exists(absolutePath))
        {
            return await ReadFileChunksAsync(request, absolutePath).ConfigureAwait(false);
        }

        return new[]
        {
            new TP3Message
            {
                Command = TP3Command.READ,
                Path = request.Path,
                Error = "NotFound"
            }
        };
    }

    private static bool TryResolvePath(IReadOnlyList<string> requestPath, out string absolutePath, out bool isRootState, out string error)
    {
        absolutePath = string.Empty;
        error = string.Empty;
        isRootState = false;

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
            isRootState = true;
            return true;
        }

        var first = segments[0].Replace('/', '\\');
        var root = first;

        if (root.Length == 2 && char.IsLetter(root[0]) && root[1] == ':')
        {
            root += "\\";
        }

        string resolved;
        if (Path.IsPathRooted(root))
        {
            resolved = root;
            for (var i = 1; i < segments.Count; i++)
            {
                resolved = Path.Combine(resolved, segments[i]);
            }
        }
        else
        {
            error = "InvalidPath";
            return false;
        }

        absolutePath = resolved;
        return true;
    }

    private static IReadOnlyList<TP3Message> BuildChunks(TP3Message request, string qid, string nodeType, byte[] data)
    {
        var results = new List<TP3Message>();

        if (data.Length == 0)
        {
            results.Add(new TP3Message
            {
                Command = TP3Command.READ,
                Path = request.Path,
                Qid = qid,
                NodeType = nodeType,
                IsChunk = true,
                ChunkIndex = 0,
                IsFinalChunk = true,
                Data = Array.Empty<byte>()
            });
            return results;
        }

        var chunkIndex = 0;
        for (var offset = 0; offset < data.Length; offset += ChunkSize)
        {
            var len = Math.Min(ChunkSize, data.Length - offset);
            var chunk = new byte[len];
            Buffer.BlockCopy(data, offset, chunk, 0, len);
            var isFinal = offset + len >= data.Length;

            results.Add(new TP3Message
            {
                Command = TP3Command.READ,
                Path = request.Path,
                Qid = qid,
                NodeType = nodeType,
                IsChunk = true,
                ChunkIndex = chunkIndex,
                IsFinalChunk = isFinal,
                Data = chunk
            });
            chunkIndex++;
        }

        return results;
    }

    private static async Task<IReadOnlyList<TP3Message>> ReadFileChunksAsync(TP3Message request, string absolutePath)
    {
        var results = new List<TP3Message>();
        await using var stream = File.OpenRead(absolutePath);
        var buffer = new byte[ChunkSize];
        var chunkIndex = 0;

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
            if (bytesRead <= 0)
            {
                if (chunkIndex == 0)
                {
                    results.Add(new TP3Message
                    {
                        Command = TP3Command.READ,
                        Path = request.Path,
                        Qid = absolutePath,
                        NodeType = "file",
                        IsChunk = true,
                        ChunkIndex = 0,
                        IsFinalChunk = true,
                        Data = Array.Empty<byte>()
                    });
                }

                break;
            }

            var chunk = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);
            var isFinal = stream.Position >= stream.Length;

            results.Add(new TP3Message
            {
                Command = TP3Command.READ,
                Path = request.Path,
                Qid = absolutePath,
                NodeType = "file",
                IsChunk = true,
                ChunkIndex = chunkIndex,
                IsFinalChunk = isFinal,
                Data = chunk
            });
            chunkIndex++;
        }

        return results;
    }
}

