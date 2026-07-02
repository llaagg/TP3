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

# Protocol

| Concept    | Type            | Scope             |
| ---------- | --------------- | ----------------- |
| `fid`      | session handle  | client connection |
| `qid_root` | object identity | server filesystem |
| `/`        | path            | namespace view    |
