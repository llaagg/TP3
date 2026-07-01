using TP3.Messages;

namespace TP3.Interfaces;

public interface IPathDataService : IService
{
    bool CanHandlePath(IReadOnlyList<string> fullPath);
    bool CanHandleQid(string qid);
    Task<TP3WalkResponse> WalkAsync(TP3WalkRequest request);
    Task<TP3ReadResponse> ReadAsync(TP3ReadRequest request);
}