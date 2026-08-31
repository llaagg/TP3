using TP3.Interfaces;
using TP3.Protocol;
using TP3.Protocol.Base;

/// <summary>
/// base namespace where all the actions will happen. Agent will use it to handle the services and mount points. 
/// </summary>
public class Namespace : BaseDirectoryNode
{
    public Namespace() : base()
    {
        this.AddChild(new Control(this));
    }

    /// <summary>
    /// Find node by  path in  namespace, if doesn't exists create it.
    /// If it exists then exception
    /// </summary>
    /// <param name="where">The path where the node should be created.</param>
    /// <returns>The created node.</returns>
    internal INode AddSubNode(string where, INode nodeToAdd)
    {
        if (string.IsNullOrWhiteSpace(where))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(where));
        }

        var segments = where.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        INode currentNode = this;

        foreach (var segment in segments)
        {
            if (currentNode is BaseDirectoryNode directoryNode)
            {
                var childNode = directoryNode.Children?.FirstOrDefault(c => c.Name == segment);
                if (childNode == null)
                {
                    // Create a new directory node if it doesn't exist
                    INode newNode;
                    if (segment == segments.Last())
                    {
                        newNode = nodeToAdd;
                    }
                    else
                    {
                        newNode = new BaseDirectoryNode(segment);
                    }
                    directoryNode.AddChild(newNode);
                    currentNode = newNode;
                }
                else
                {
                    currentNode = childNode;
                }
            }
            else
            {
                throw new InvalidOperationException($"Cannot create a child node under a non-directory node: {currentNode.Name}");
            }
        }
        return currentNode;
    }
}

public class Control : BaseDirectoryNode
{
    public Control(Namespace ns) : base()
    {
        this.AddChild(new Mount(ns));
    }
}

internal class Mount : BaseControlParamsArgsCommand
{
    private readonly Namespace _namespace;

    public Mount(Namespace ns) : base()
    {
        _namespace = ns;
    }

    protected override Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        if (args == null || args.Length < 2)
        {
            throw new ArgumentException("Mount command requires at least two arguments: the target location and the path to mount.");
        }

        string where = args[0];
        string pathToMount = args[1];

        // Here you would implement the logic to mount the specified path.
        // For demonstration purposes, we'll just write a message to the output stream.

        /// find recursive by path in where
        var targetNode = _namespace.AddSubNode(where, new MountPoint(pathToMount));
        if (targetNode == null)
        {
            throw new ArgumentException($"Target location '{where}' not found in the namespace.");
        }
        
        
        
        using var writer = new StreamWriter(output, leaveOpen: true)
        {
            AutoFlush = true
        };
        writer.WriteLine($"Mounting path: {pathToMount} to location: {where}");

        return Task.CompletedTask;
    }
}

internal class MountPoint : BaseDirectoryNode
{
    public string PathToMount { get; }
    public MountPoint(string pathToMount) : base(pathToMount)
    {
        this.PathToMount = pathToMount;
    }
}