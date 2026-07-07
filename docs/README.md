# TP3 Project Summary

TP3 is a tree-based protocol and runtime for exposing resources (files, directories, service nodes) over a tagged request/response session.

This document keeps only the essential information for understanding and working on the codebase.

## What TP3 Does

- Exposes a node tree where each node is a `File` or `Directory`.
- Uses a stateful, tag-based session model (`tag` -> pointer/node binding).
- Supports navigation and data access through a small command set.
- Serves both local filesystem-backed and service-backed nodes.

## Core Protocol Flow

The practical flow (validated in `src/TP3.Tests/FlowTest.cs`) is:

1. `ATTACH(tag)` to bind a session handle to the root.
2. `WALK(tag, newTag, path...)` to navigate and create/rebind handles.
3. `OPEN(tag)` to open the current node.
4. `READ(tag, offset, maxBytes)` to stream directory/file content.
5. `CLUNK(tag)` to release the handle.

### Read Semantics

- Directory read: concatenated `TP3StatPayload` JSON objects.
- File read: raw bytes.
- Pagination/chunking: repeat `READ` with increasing `offset` until returned size is `0` or less than requested `maxBytes`.

## Architecture (High Level)

1. Interfaces and contracts: `src/TP3.Interfaces`.
2. Protocol serialization and stream helpers: `src/TP3.Protocol`.
3. Host/agent/session/transport logic: `src/TP3.Logic`.
4. Service implementations:
	- Filesystem: `src/TP3.Service.FileSystem`
	- Remote/control tree: `src/TP3.Service.Remote`
5. Executables:
	- Server: `src/TP3.Server`
	- CLI: `src/TP3.CLI`
	- GUI frontends: `src/TP3.GUI.*`

## Test Coverage Snapshot

- `FlowTest`: end-to-end attach/walk/open/read/clunk behavior.
- `DirectoryTests`: directory stream reading and offset behavior.
- `SessionManagementTests`: tag reuse rules (`attach` collision and reuse after `clunk`).
- `ServerFlow`: placeholder scenarios for server-to-server behavior (not fully implemented).

## Build Entry Points

- Main solution: `src/TP3.slnx`
- Full solution with GUI: `src/TP3.WithGUI.slnx`

Common builds:

- `dotnet build src/TP3.slnx`
- `dotnet build src/TP3.WithGUI.slnx`

## Current Important Constraints

- Session/tag lifecycle is core correctness logic; avoid bypassing `attach`/`clunk` rules.
- Directory reads are stream-oriented and may arrive in chunks.
- Keep protocol/message contracts synchronized across `Interfaces`, `Protocol`, and `Logic`.
