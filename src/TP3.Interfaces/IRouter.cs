using TP3.Messages;

namespace TP3.Interfaces;

public interface IRouter
{
    /// <summary>
    /// Responds to a specific request with a given message.
    /// </summary>
    /// <param name="agent">who</param>
    /// <param name="message">to what</param>
    /// <param name="tP3Transport">with what transport</param>
    /// <returns></returns>
    Task Respond(IAgent agent, TP3Message message, INetworkPipe targetTransport);
    Task Route(INetworkPipe sourceTransport, TP3Message message);
}
