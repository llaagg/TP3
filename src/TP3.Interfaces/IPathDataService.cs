using TP3.Messages;

namespace TP3.Interfaces;

public interface IPathDataService : IService
{
    bool CanHandlePath(IReadOnlyList<string> fullPath);
    INode? ResolvePath(IReadOnlyList<string> fullPath);
    ServiceReader CreateReader(INode node);
}