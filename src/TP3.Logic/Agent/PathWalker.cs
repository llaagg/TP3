using System;
using System.Collections.Generic;
using System.Linq;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Agent;

public sealed class PathWalker
{
    private readonly Func<IEnumerable<IPathDataService>> servicesProvider;
    private readonly Dictionary<string, RegisteredQid> nodesByQid = new(StringComparer.OrdinalIgnoreCase);
    private readonly object sync = new();

    public PathWalker(Func<IEnumerable<IPathDataService>> servicesProvider)
    {
        this.servicesProvider = servicesProvider ?? throw new ArgumentNullException(nameof(servicesProvider));
    }

    public Task<TP3WalkResponse> WalkAsync(TP3WalkRequest request)
    {
        var service = ResolvePathService(request.Args);
        if (service is null)
        {
            return Task.FromResult(new TP3WalkResponse
            {
                Args = request.Args,
                Tag = request.Tag,
                Error = "NotFound"
            });
        }

        var node = service.ResolvePath(request.Args);
        if (node is null)
        {
            return Task.FromResult(new TP3WalkResponse
            {
                Args = request.Args,
                Tag = request.Tag,
                Error = "NotFound"
            });
        }

        var registered = EnsureRegisteredNode(node, service);

        return Task.FromResult(new TP3WalkResponse
        {
            Args = request.Args,
            Tag = request.Tag,
            Qid = node.Qid,
            NodeType = registered.NodeType
        });
    }

    public Task<TP3ReadResponse> ReadAsync(TP3ReadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Qid))
        {
            return Task.FromResult(new TP3ReadResponse
            {
                Args = request.Args,
                Tag = request.Tag,
                Error = "QidRequired"
            });
        }

        RegisteredQid? registered;
        lock (sync)
        {
            nodesByQid.TryGetValue(request.Qid!, out registered);
        }

        if (registered is null)
        {
            return Task.FromResult(new TP3ReadResponse
            {
                Args = request.Args,
                Tag = request.Tag,
                Qid = request.Qid,
                Error = "NotFound"
            });
        }

        return ReaderChunkerEngine.ReadAsync(request, request.Qid!, registered.NodeType, registered.Reader);
    }

    private IPathDataService? ResolvePathService(IReadOnlyList<string> requestPath)
    {
        return servicesProvider()
            .FirstOrDefault(s => s.CanHandlePath(requestPath));
    }

    private RegisteredQid EnsureRegisteredNode(INode node, IPathDataService service)
    {
        lock (sync)
        {
            if (nodesByQid.TryGetValue(node.Qid, out var existing))
            {
                return existing;
            }

            var nodeType = DetermineNodeType(node);
            var reader = service.CreateReader(node);
            existing = new RegisteredQid
            {
                Node = node,
                Service = service,
                NodeType = nodeType,
                Reader = reader
            };

            nodesByQid[node.Qid] = existing;
            return existing;
        }
    }

    private static NodeType DetermineNodeType(INode node)
    {
        if (node.Children is null)
        {
            return NodeType.File;
        }

        foreach (var child in node.Children)
        {
            if (child is not null)
            {
                return NodeType.Directory;
            }
        }

        return NodeType.File;
    }

    private sealed class RegisteredQid
    {
        public required INode Node { get; init; }
        public required IPathDataService Service { get; init; }
        public required NodeType NodeType { get; init; }
        public required ServiceReader Reader { get; init; }
    }
}
