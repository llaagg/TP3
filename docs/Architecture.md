# Architecture

TP3 is a tree-oriented resource protocol with a small command surface and stateful session handles.

It is best understood as a conceptual 9P-inspired runtime over a virtual tree namespace, not as a strict Plan 9 wire clone.

## Layer View

1. Application clients (`TP3.CLI`, `TP3.Server`, GUI frontends)
2. TP3 logic (routing, sessions, agent host, transports)
3. Service layer (filesystem and remote nodes)
4. Protocol layer (message contracts, serialization, read stream helpers)
5. OS/runtime resources (filesystem and process environment)

## Resource Model

- Node is the core abstraction.
- Node types: `Directory`, `File`, and command-like nodes.
- Services expose node trees under a unified namespace.
- Nodes may be backed by real files or pure runtime/service state.

## Protocol Concepts

| Concept | Meaning | Scope |
| --- | --- | --- |
| `tag` | Session handle for a bound pointer | Per client connection |
| `node/qid-like identity` | Resolved node identity in responses | Server namespace |
| `path segments` | Navigation input for `WALK` | Request payload |
| `CRUD`+ `SEARCH` | by convention we will have crud operations on nodes | Commands in control plane |

## Command Lifecycle

1. `ATTACH` -> bind handle to root.
2. `WALK` -> move/rebind to target path.
3. `OPEN` -> prepare node for read access.
4. `READ` / `WRITE` -> stream directory entries, file bytes, or command I/O.
5. `CLUNK` -> release handle.

## Behavior Notes

- Directory reads return serialized stat payloads.
- File reads return raw bytes.
- Chunked reads use `offset + maxBytes` until EOF.
