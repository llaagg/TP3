# TP3 Project Summary

TP3 is a conceptual, tree-oriented protocol runtime inspired by 9P-style interactions.

The main idea is simple: expose a virtual tree of nodes and interact with those nodes using a small set of stateful commands (`ATTACH`, `WALK`, `OPEN`, `READ`, `WRITE`, `CLUNK`).

## What TP3 Is

- A protocol and runtime that treats resources as a navigable tree.
- A virtual namespace where nodes can represent filesystem entries, service state, commands, or remote resources.
- A tagged session model where `tag` identifies a client-side handle/pointer.

## What TP3 Is Not

- Not a strict wire-compatible implementation of Plan 9 9P.
- Not limited to a physical filesystem tree.
- Not just a transport format; the project also includes host/session/service runtime behavior.

## Core Concept

TP3 follows the conceptual 9P pattern: "everything is a node in a tree" and clients operate by walking to a node, opening it, then reading or writing bytes.

The tree is virtual:

- Structure is provided by services.
- Some branches map to real files.
- Other branches map to logical/runtime objects.
- The same client flow applies regardless of backing implementation.

## Practical Protocol Flow

Validated in tests (notably `src/TP3.Tests/FlowTest.cs`):

1. `ATTACH(tag)` binds a handle to the root namespace.
2. `WALK(tag, newTag, path...)` navigates and optionally rebinds to `newTag`.
3. `OPEN(tag)` prepares current node data stream semantics.
4. `READ(tag, offset, maxBytes)` or `WRITE(tag, offset, data)` performs data operations.
5. `CLUNK(tag)` releases the handle.

## Read/Write Semantics

- Directory reads return serialized node metadata payloads.
- File and command-like nodes return raw bytes.
- Offset-based chunking is expected; clients repeat reads until EOF.

## Solution Map

1. `src/TP3.Interfaces`: protocol contracts and abstractions.
2. `src/TP3.Protocol`: protobuf messages, serialization, and stream helpers.
3. `src/TP3.Logic`: agent/host/session/transport behavior.
4. `src/TP3.Service.*`: concrete tree providers (filesystem, IPC, remote, WebDav, attached, etc.).
5. Frontends and runtimes:
   - `src/TP3.Server`
   - `src/TP3.CLI`
   - `src/TP3.GUI.*`

## Build Entry Points

- Main solution: `src/TP3.slnx`
- MAUI GUI project: `src/TP3.GUI.Maui/TP3.GUI.Maui.csproj`

## Additional Docs

- `Architecture.md`: layered architecture and command lifecycle.
- `ClientFlows.md`: concrete client interaction patterns.
