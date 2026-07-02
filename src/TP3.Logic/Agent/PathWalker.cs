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

    public async Task<TP3WalkResponse> WalkAsync(TP3WalkRequest request)
    {
        var nodes = ResolveNode(trunk, request.Path);
        var registered = EnsureRegisteredNodes(nodes).ToList();

        return new TP3WalkResponse
        {
            Tag = request.Tag,
            Infos = registered
        };
    }

    public Task<TP3ReadResponse> ReadAsync(INetworkPipe incomingTransport, TP3ReadRequest request)
    {
        var node = incomingTransport.TP3Transport.GetNode(incomingTransport, request.Tag);
        
        



        // if (string.IsNullOrWhiteSpace(request.Qid))
        // {
        //     return Task.FromResult(new TP3ReadResponse
        //     {
        //         Tag = request.Tag,
        //         Error = "QidRequired"
        //     });
        // }

        // RegisteredQid? registered;
        // lock (sync)
        // {
        //     nodesByQid.TryGetValue(request.Qid!, out registered);
        // }

        // if (registered is null)
        // {
        //     return Task.FromResult(new TP3ReadResponse
        //     {
        //         Tag = request.Tag,
        //         Qid = request.Qid,
        //         Error = "NotFound"
        //     });
        // }



        return null;
    }

    private IEnumerable<INode> ResolveNode(INode trunk, IList<string> requestPath)
    {
        var segments = requestPath
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        INode? current = trunk;
        // trunk because why not
        yield return current;

        foreach (var segment in segments)
        {
            if (current.Children is null)
            {
                yield break;
            }

            current = current?.Children.FirstOrDefault(child => string.Equals(child?.Name, segment, StringComparison.OrdinalIgnoreCase));

            if (current is null)
            {
                yield break;
            }

            yield return current;
        }
    }

    private IEnumerable<NodeInfo> EnsureRegisteredNodes(IEnumerable<INode> nodes)
    {
        var result = nodes?.ToList().Select(child => EnsureRegisteredNode(child));

        return result ?? Enumerable.Empty<NodeInfo>();
    }

    private NodeInfo EnsureRegisteredNode(INode node)
    {
        lock (sync)
        {
            if (nodesByQid.TryGetValue(node.Id, out var existing))
            {
                return new NodeInfo
                {
                    Id = existing.Node.Id,
                    NodeType = existing.NodeType
                };
            }

            existing = new RegisteredQid
            {
                Node = node,
                NodeType = node.NodeType,
            };

            nodesByQid[node.Id] = existing;
            return new NodeInfo
            {
                Id = existing.Node.Id,
                NodeType = existing.NodeType
            };
        }
    }

    private sealed class RegisteredQid
    {
        public required INode Node { get; init; }
        public required NodeType NodeType { get; init; }
    }
}
