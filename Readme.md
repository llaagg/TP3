# TP3

TP3 is a conceptual implementation of a 9P-like tree protocol.

The main idea is that TP3 can evolve into a user-space state and control plane across all devices where TP3 runs, allowing one virtual tree model to connect and operate heterogeneous endpoints.

Target device landscape includes PCs, Android devices (including Android TV), and iOS devices.

The project exposes a virtual node tree and lets clients navigate and operate through stateful tagged commands (`ATTACH`, `WALK`, `OPEN`, `READ`, `WRITE`, `CLUNK`).

In practice, TP3 is a runtime + protocol stack:

- protocol contracts and serialization
- session and transport logic
- service-backed virtual tree providers (filesystem, remote, IPC, and others)
- server, CLI, and GUI frontends

TP3 focuses on the conceptual model (tree + handles + byte streams), not strict Plan 9 wire compatibility.

## Read Next

- `docs/README.md`: solution summary.
- `docs/Architecture.md`: architecture layers and lifecycle.
- `docs/ClientFlows.md`: practical client flows.
