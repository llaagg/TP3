using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Agent;

/// <summary>
/// Long time thread, listens for incoming connections and handles them.
/// Delivers trunk to the clients connected to it. (that should be other agents)
/// Connects to other agents and requests trunk from them.  
/// </summary>
public class Agent : IAgent
{
    private readonly IRouter router;
    private readonly ILogger? logger;

    public Agent(IRouter router, ILogger? logger = null)
    {
        this.router = router;
        this.logger = logger;

        logger?.LogInformation("Initializing agent logic.");
    }

    public INode T
    {
        get
        {
            return new Trunk(this.Services);
        }
    }

    public List<IService> Services { get; private set; } = new List<IService>();

    public async Task AddService(IService service)
    {
        try
        {
            await service.Init(this);
            this.Services.Add(service);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize service: {service.GetType().Name}", ex);
        }
    }

    public virtual void Dispose()
    {
        // Agent logic has no transport of its own.
    }


    private INode Navigate(INode node, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return node;
        }

        var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var currentNode = node;

        foreach (var segment in segments)
        {
            if (currentNode.Children == null)
            {
                logger?.LogWarning("Node '{NodeName}' has no children. Cannot navigate to '{Segment}'.", currentNode.Name, segment);
                return new ZeroNodesNode();
            }

            var nextNode = currentNode.Children.FirstOrDefault(c => c.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
            if (nextNode == null)
            {
                logger?.LogWarning("Child node '{Segment}' not found under '{NodeName}'.", segment, currentNode.Name);
                return new ZeroNodesNode();
            }

            currentNode = nextNode;
        }

        return currentNode;
    }

    private string NodeChildrenToString(INode node)
    {
        var children = node.Children?.ToList() ?? new List<INode>();

        var result = children.Any()
            ? string.Join(Environment.NewLine, children.Select(c => c.Name))
            : "(empty)";

        return result;
    }

    public async Task Handle(TP3Message request)
    {
        if (request == null)
        {
            logger?.LogWarning("Received null TP3 message.");
            return;
        }
        if (request.Command == TP3Command.NONE)
        {
            logger?.LogWarning("Received {Command} TP3 message.", request.Command);
            return;
        }
        if (request.Command == TP3Command.LIST)
        {
            var targetNode = Navigate(T, request.Path);
            var childrenList = NodeChildrenToString(targetNode);

            logger?.LogInformation("LIST command received for target '{Target}'. Children: {Children}", request.Path, childrenList.Count());
#warning TODO: return the list of children to the requester

            // let's send the response
            await this.Respond(
                request,
                new TP3Message
                {
                    Command = TP3Command.LISTRESPONSE,
                    Path = request.Path,
                    Payload = childrenList
                });
            return;
        }
        if (request.Command == TP3Command.ECHO)
        {
            logger?.LogInformation("ECHO command received with payload: {Payload}", request.Payload);
            return;
        }
    }

    private async Task Respond(TP3Message request, TP3Message tP3Message)
    {
        await router.Respond(this, request, tP3Message);
    }
}

internal class ZeroNodesNode : INode
{
    public string Name => "EmptyNode";
    public IEnumerable<INode> Children => Enumerable.Empty<INode>();
    public Stream Data => Stream.Null;
}