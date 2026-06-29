namespace TP3.Maui.Client;

// AgentNode is an in-app agent instance that runs inside the MAUI app process.
// It exposes fast internal method calls to the app UI without IPC overhead.
public class AgentNode
{
    public MetaData Meta { get; } = new();

    public AgentNode()
    {
        // initialize agent state
    }

    // Example synchronous internal API (no serialization overhead)
    public void SendCommand(string command, object? payload = null)
    {
        // handle command
    }

    // Example async API
    public System.Threading.Tasks.Task<object?> InvokeAsync(string name, object? args = null)
    {
        // placeholder implementation
        return System.Threading.Tasks.Task.FromResult<object?>(null);
    }
}
