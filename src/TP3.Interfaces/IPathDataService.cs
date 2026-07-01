using TP3.Messages;

namespace TP3.Interfaces;

public interface IPathDataService : IService
{
    bool CanHandlePath(IReadOnlyList<string> fullPath);
    bool CanHandleQid(string qid);
    Task<TP3Message> WalkAsync(TP3Message request);
    Task<TP3Message> ReadAsync(TP3Message request);
}