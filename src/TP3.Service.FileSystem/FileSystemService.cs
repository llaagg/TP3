using System.Text;
using System.Text.Json;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.FileSystem;

/// <summary>
/// Service that allows access to filesystem, in tp3 space
/// </summary>
public class FileSystemService : IPathDataService
{
    private readonly Dictionary<string, RegisteredNode> nodesByQid = new(StringComparer.OrdinalIgnoreCase);
    private readonly object sync = new();

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

    public bool CanHandleQid(string qid)
    {
        lock (sync)
        {
            return nodesByQid.ContainsKey(qid);
        }
    }

    public Task<TP3Message> WalkAsync(TP3Message request)
    {
        if (!TryResolvePath(request.Args, out var absolutePath, out var isRootState, out var error))
        {
            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Args = request.Args,
                Error = error
            });
        }

        if (isRootState)
        {
            var qid = CreateQid();
            var node = new StateNode(qid);
            RegisterNode(qid, node, NodeType.Directory, new DirectoryJsonReader(isRootState: true, absolutePath: null));

            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Args = request.Args,
                Tag = request.Tag,
                Qid = qid,
                NodeType = NodeType.Directory
            });
        }

        if (Directory.Exists(absolutePath))
        {
            var qid = CreateQid();
            var node = new FileSystemNode(absolutePath, qid);
            RegisterNode(qid, node, NodeType.Directory, new DirectoryJsonReader(isRootState: false, absolutePath));

            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Args = request.Args,
                Tag = request.Tag,
                Qid = qid,
                NodeType = NodeType.Directory
            });
        }

        if (File.Exists(absolutePath))
        {
            var qid = CreateQid();
            var node = new FileSystemNode(absolutePath, qid);
            RegisterNode(qid, node, NodeType.File, new FileBinaryReader(absolutePath));

            return Task.FromResult(new TP3Message
            {
                Command = TP3Command.WALK,
                Args = request.Args,
                Tag = request.Tag,
                Qid = qid,
                NodeType = NodeType.File
            });
        }

        return Task.FromResult(new TP3Message
        {
            Command = TP3Command.WALK,
            Args = request.Args,
            Error = "NotFound"
        });
    }

    public async Task<TP3Message> ReadAsync(TP3Message request)
    {
        if (string.IsNullOrWhiteSpace(request.Qid))
        {
            return new TP3Message
            {
                Command = TP3Command.READ,
                Args = request.Args,
                Tag = request.Tag,
                Error = "QidRequired"
            };
        }

        RegisteredNode? registered;
        lock (sync)
        {
            nodesByQid.TryGetValue(request.Qid, out registered);
        }

        if (registered is null)
        {
            return new TP3Message
            {
                Command = TP3Command.READ,
                Args = request.Args,
                Tag = request.Tag,
                Qid = request.Qid,
                Error = "NotFound"
            };
        }

        return await ReaderChunkerEngine
            .ReadAsync(request, request.Qid!, registered.NodeType, registered.Reader)
            .ConfigureAwait(false);
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

    private static string CreateQid()
    {
        return $"fs:{Guid.NewGuid():N}";
    }

    private void RegisterNode(string qid, INode node, NodeType nodeType, ServiceReader reader)
    {
        lock (sync)
        {
            nodesByQid[qid] = new RegisteredNode
            {
                Node = node,
                NodeType = nodeType,
                Reader = reader
            };
        }
    }

    private sealed class RegisteredNode
    {
        public required INode Node { get; init; }
        public required NodeType NodeType { get; init; }
        public required ServiceReader Reader { get; init; }
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

