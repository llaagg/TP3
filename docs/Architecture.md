# Architecture

TP3 is a tree-oriented resource protocol with a small command surface and stateful session handles.

## Layer View

1. Application clients (`TP3.CLI`, `TP3.Server`, GUI frontends)
2. TP3 logic (routing, sessions, agent host, transports)
3. Service layer (filesystem and remote nodes)
4. Protocol layer (message contracts, serialization, read stream helpers)
5. OS/runtime resources (filesystem and process environment)

## Resource Model

- Node is the core abstraction.
- Node types: `Directory` and `File`.
- Services expose node trees under a unified namespace.

## Protocol Concepts

| Concept | Meaning | Scope |
| --- | --- | --- |
| `tag` | Session handle for a bound pointer | Per client connection |
| `node/qid-like identity` | Resolved node identity in responses | Server namespace |
| `path segments` | Navigation input for `WALK` | Request payload |

## Command Lifecycle

1. `ATTACH` -> bind handle to root.
2. `WALK` -> move/rebind to target path.
3. `OPEN` -> prepare node for read access.
4. `READ` -> stream directory entries or file bytes.
5. `CLUNK` -> release handle.

## Behavior Notes

- Directory reads return serialized stat payloads.
- File reads return raw bytes.
- Chunked reads use `offset + maxBytes` until EOF.
