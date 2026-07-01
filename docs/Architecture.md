# Layers
```Application
      │
Filesystem View
      │
Capability Layer (3+C)
      │
OS Adapter / Device Driver
      │
Windows / Linux / Android / ESP32
```

# TP3

Node has resources.

Resources has:
* State (observable properties and streams)
* Control (operations)
* Events (subscriptions)
* Coordination (security and consistency)

# Use cases

```
                  
                  Resource
                        │
            ┌───────────┴───────────┐
            │                       │
      Capabilities             Metadata
      (State/Control/Events)     (type, name, etc.)
            │
            ├──────────────┬──────────────┬──────────────┐
            │              │              │              │
      Filesystem View    .NET SDK      REST API           CLI
```

# Arcitecture

## Network architecture from `AgentHost` perspective

From `AgentHost`, TP3 networking is organized into three layers:

1. **Network layer**
   * Handles transport and connection lifecycle
   * Includes `TCPTransport` for external TCP clients
   * Includes `IpcTransport` for local loopback IPC clients
   * Abstracted by `INetworkTransport` and wrapped by `TP3Transport`

2. **Protocol layer**
   * Parses raw request text into TP3 protocol objects
   * Uses `TP3Protocol.Parse(...)` to build `TP3Message`
   * Encodes `Command`, `Target`, and `Payload`
   * Keeps the network transport implementation agnostic

3. **Node layer**
   * Routes and handles protocol messages
   * Implemented by `Router`, `Agent`, `INode`, and services
   * Executes commands via `Agent.HandleRequest(...)`
   * Represents application nodes and resource behavior

### Request flow

- Client connects via TCP or IPC
- The **network layer** reads raw request text
- The **protocol layer** parses it into `TP3Message`
- The **node layer** routes and executes the message
- The response is returned back through the layers

# Agent has

INode State

public interface INode
{
    string Name { get; }
    public IEnumerable<INode>? Children { get; }
    public Stream? Data { get; }
}