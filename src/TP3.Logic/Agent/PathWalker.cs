using System;
using System.Collections.Generic;
using System.Linq;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Agent;

public sealed class PathWalker
{
    private readonly Dictionary<string, RegisteredQid> nodesByQid = new(StringComparer.OrdinalIgnoreCase);
    private readonly object sync = new();
    private readonly INode trunk;

    public PathWalker(INode trunk)
    {
        this.trunk = trunk;
    }

    public Task<TP3WalkResponse> WalkAsync(TP3WalkRequest request)
    {

        var node = ResolveNode(trunk, request.Args);
        if (node is null)
        {
            return Task.FromResult(new TP3WalkResponse
            {
                Args = request.Args,
                Tag = request.Tag,
                Error = "NotFound"
            });
        }

        var registered = EnsureRegisteredNode(node);

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

        return ReaderChunkerEngine.ReadAsync(request, request.Qid!, registered.NodeType, registered.Node.Reader);
    }

    private INode? ResolveNode(INode trunk, IReadOnlyList<string> requestPath)
    {
        var segments = requestPath
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        INode current = trunk;
        foreach (var segment in segments)
        {
            if (current.Children is null)
            {
                return null;
            }

            current = current.Children.FirstOrDefault(child => string.Equals(child?.Name, segment, StringComparison.OrdinalIgnoreCase));
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    private RegisteredQid EnsureRegisteredNode(INode node)
    {
        lock (sync)
        {
            if (nodesByQid.TryGetValue(node.Qid, out var existing))
            {
                return existing;
            }

            existing = new RegisteredQid
            {
                Node = node,
                NodeType = node.NodeType,
            };

            nodesByQid[node.Qid] = existing;
            return existing;
        }
    }

    private sealed class RegisteredQid
    {
        public required INode Node { get; init; }
        public required NodeType NodeType { get; init; }
    }
}
