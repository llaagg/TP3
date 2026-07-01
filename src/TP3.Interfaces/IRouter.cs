using TP3.Messages;

namespace TP3.Interfaces;

public interface IRouter
{
    /// <summary>
    /// Responds to a specific request with a given message.
    /// </summary>
    /// <param name="agent">who</param>
    /// <param name="request">to what</param>
    /// <param name="tP3Message">with what</param>
    /// <returns></returns>
    Task Respond(IAgent agent, TP3Message request, TP3Message tP3Message);
    Task Route(INetworkTransport ipcTransport, TP3Message message);
}
