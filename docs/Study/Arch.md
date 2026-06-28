# Architecture Design: Compute + Streams (C+S)

## 1. Purpose

Define a unified distributed architecture where every node exposes:

- Compute capabilities
- Streams for data and events

This model is designed to work consistently across desktops, servers, mobile devices, and microcontrollers.

---

## 2. Core Principle

Each machine is a node.
Each node publishes a stable contract with two primitives:

1. Compute
2. Streams

All interaction in the system becomes:

- read from stream
- write to stream
- subscribe to event stream
- invoke compute

---

## 3. Stream Types

### 3.1 Read Stream

Use for consuming data similar to file reads.

Properties:
- May be bounded (snapshot)
- May be unbounded (tail/live)
- Supports offset/cursor-based reads

Examples:
- sensor snapshot
- log tail
- file-like content read

### 3.2 Write Stream

Use for sending data or commands into a node.

Properties:
- Supports append and overwrite modes
- May trigger compute actions
- Should return status/ack where required

Examples:
- set GPIO value
- write configuration
- send command payload

### 3.3 Event Stream

Use for continuous asynchronous notifications.

Properties:
- Unbounded by default
- Ordered per stream
- Consumer reads indefinitely

Examples:
- motion detected
- process exited
- network changed

---

## 4. Compute Surface

Compute is explicit and callable as named operations.

Properties:
- Deterministic operation signature
- Input/output schema
- Timeout and retry policy
- Authorization scope

Examples:
- transform data
- execute routine
- run diagnostics

---

## 5. Node Contract

Each node should expose the following logical interface:

- streams/list
- streams/read/{name}
- streams/write/{name}
- streams/events/{name}
- compute/invoke/{name}
- node/meta
- node/health

Recommended metadata fields:
- node_id
- node_type
- capabilities
- protocol_versions
- auth_mode

---

## 6. Coordination Model

Coordination is carried by metadata and policy attached to streams and compute calls.

Required coordination primitives:
- Identity: who produced/requested
- Ordering: sequence and causality
- Ownership: who can mutate
- Consistency: source-of-truth and conflict handling
- Authorization: who can read/write/invoke

Mechanisms:
- sequence numbers
- monotonic timestamps
- idempotency keys
- leases/locks for exclusive control

---

## 7. Mapping to 3+C

C+S maps to 3+C as follows:

- State = durable streams
- Control = write streams and compute invoke
- Events = event streams
- Coordination = metadata, policy, and ordering rules

This preserves prior design language while simplifying implementation thinking.

---

## 8. Transport Strategy

### 8.1 MQTT-First (MVP)

Use MQTT as default transport for rapid prototyping and IoT compatibility.

Strengths:
- Natural topic tree
- Pub/sub events
- Retained state messages
- Lightweight clients (ESP/Arduino class devices)

Typical mapping:
- node/{id}/state/{stream}
- node/{id}/control/{stream}
- node/{id}/events/{stream}
- node/{id}/coord/{stream}

### 8.2 Hybrid Evolution

Use specialized channels when needed:

- MQTT for events and lightweight state/control
- gRPC/TCP for high-throughput compute and streaming
- File sync/object store for large durable state

---

## 9. Security Baseline

Security is mandatory from day one.

- mTLS for node-to-node channels
- Per-node identity and short-lived credentials
- Topic/path-level ACLs
- Signed control envelopes (nonce + timestamp + expiry)
- Replay protection and idempotency checks
- Immutable audit stream for critical operations
- Key rotation and revocation workflow

PGP/OpenPGP can be used for artifact signing and offline trust chains, but runtime transport security should rely on TLS/mTLS.

---

## 10. Reference Paths

Global namespace example:

/
  nodes/
    laptop-01/
      streams/
      compute/
      coord/
    phone-01/
      streams/
      compute/
      coord/
    esp32-01/
      streams/
      compute/
      coord/

Microcontroller node profile example:

- streams/read/temperature
- streams/read/humidity
- streams/write/gpio
- streams/events/button
- compute/invoke/restart

---

## 11. Delivery Phases

### Phase 1: Contract and Topics
- finalize node contract
- finalize naming/path conventions
- define stream envelope schema

### Phase 2: Two-Node Prototype
- one desktop node agent
- one ESP32 node adapter
- end-to-end read/write/event flow

### Phase 3: Coordination and Policy
- ACL and identity model
- ordering/idempotency guarantees
- audit and observability

### Phase 4: Hybrid Performance Paths
- add gRPC/TCP for heavy compute streams
- benchmark latency and throughput
- tune per-workload transport selection

---

## 12. Success Criteria

- Any node can expose data as streams with discoverable metadata.
- Any authorized node can invoke compute on another node.
- Event streams can be consumed continuously without polling.
- Coordination rules prevent unsafe concurrent control.
- Same architecture works for desktop/server/mobile/MCU classes.

---

## 13. Namespace Model (Plan 9 Influence)

Namespace turns streams into an operating abstraction instead of a flat set of channels.

### 13.1 Namespace Layers

1. Global namespace
  - Unified logical tree for all nodes in a deployment.
  - Example: /world/site-a/nodes/esp32-01/...

2. Local namespace
  - Resources physically owned by a node.
  - Example: /local/streams/read/temperature

3. Mounted namespace
  - Remote branches attached into local view.
  - Lets each node compose a task-specific view of many nodes.

4. Policy namespace
  - Access and ownership rules attached to path prefixes.
  - Inheritance applies from parent path to child path.

### 13.2 Trunk Integration

Each node has one namespace root stream group called trunk.

- /nodes/{node_id}/trunk/meta
- /nodes/{node_id}/trunk/control/*
- /nodes/{node_id}/trunk/state/*
- /nodes/{node_id}/trunk/events/*
- /nodes/{node_id}/trunk/compute/*

Trunk is the anchor for discovery, policy, and routing. It should fan out into substreams and not be implemented as one giant mixed stream.

### 13.3 Mount Rules

- A mount maps a remote namespace prefix into a local prefix.
- Mounts are explicit and versioned.
- Mounts can be read-only or read-write.
- Mounts carry trust metadata (issuer, cert chain, policy set).
- Conflicts are resolved using priority + explicit override rules.

### 13.4 Namespace Contract Stability

- Paths are treated as APIs and must remain stable.
- Breaking path changes require versioned prefixes.
- Capability discovery must expose supported namespace versions.
- Deprecation windows should be declared in node metadata.

### 13.5 Example Composed View

A desktop node can mount:

- local sensors from /local/...
- phone control branch from /remote/phone-01/trunk/control/...
- esp event branch from /remote/esp32-01/trunk/events/...

This creates a single operational view while preserving ownership and policy boundaries.

---

## 14. High-Level Concept Projections

The internal runtime should stay uniform (streams + compute), but nodes may expose higher-level concepts where it improves usability and interoperability.

### 14.1 Projection Principle

- Core truth is stream-native.
- High-level concepts are projections over stream/compute contracts.
- Projections are optional and capability-driven.
- No projection may bypass coordination, policy, or audit.

### 14.2 Filesystem Projection

Expose stream-backed resources as file-like trees when useful.

- Read stream -> file read or tail
- Write stream -> file write or append
- Event stream -> file notification feed
- Compute invoke -> executable-like control endpoint

This enables familiar tooling while preserving stream semantics.

### 14.3 D-Bus or Message-Bus Projection

Expose stream/compute endpoints as bus methods and signals.

- Method call maps to compute invoke or write stream command
- Signal maps to event stream publish
- Property read/write maps to state stream materialization

This is useful for Linux desktop/service integration and existing automation ecosystems.

### 14.4 Native Event Framework Projection

Map event streams into platform-native systems where available.

- Linux: epoll/signalfd-oriented adapters
- Windows: ETW and message loop adapters
- Android: intent/binder adapters
- macOS: CFRunLoop/XPC adapters

Adapters should preserve event IDs, ordering keys, and causal metadata.

### 14.5 Database Projection

Expose durable stream views as queryable tables/collections.

- Stream log -> append-only event table
- Materialized state -> key/value or document projection
- Compute result streams -> queryable job/result views

Database projection is a read/write facade and must not become the source of truth unless explicitly configured.

### 14.6 Capability Advertisement

Each node should publish available projections in metadata:

- node/projections/filesystem
- node/projections/dbus
- node/projections/events
- node/projections/database

Clients choose the best projection they support, with stream-native access always available as fallback.
