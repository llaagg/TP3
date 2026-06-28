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

Trunk is the opinionated anchor for discovery, policy, and routing. It defines a stable, consistent way to access a node's core resources and makes common operations predictable.

Namespace outside trunk should remain flexible and extensible:

- custom mounts under /nodes/{node_id}/mounts/*
- app-specific views under /namespaces/{namespace_name}/...
- projections and overlays defined by policy or capability

This means:

- trunk is the canonical entry point
- namespace is the freedom layer for composition, merging, and specialized views
- both coexist without forcing every resource into the same rigid schema

### 13.2.1 Example: Trunk + Custom Namespaces

A node exposes a fixed trunk, and apps can request namespaces that combine branches from multiple devices.

- /nodes/laptop-01/trunk/meta
- /nodes/laptop-01/trunk/control/*
- /nodes/laptop-01/trunk/state/*
- /nodes/laptop-01/trunk/events/*
- /nodes/laptop-01/trunk/compute/*

Custom namespaces:

- /namespaces/photos-all/ -> union of /nodes/phone-01/trunk/state/photos and /nodes/camera-02/trunk/state/photos
- /namespaces/office/ -> merged view of cloud file storage and laptop documents
- /namespaces/phone-events/ -> mounted /nodes/phone-01/trunk/events/notifications

This keeps `trunk` stable and opinionated while allowing flexible, application-specific namespace composition.

Trunk should fan out into substreams and not be implemented as one giant mixed stream.

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

---

## 15. Namespace Storage, Configuration, and Load

Namespaces should be represented as data, not hardcoded behavior.

### 15.1 What Gets Stored

Store namespace-related data in three layers:

1. Namespace manifest
  - Declares paths, mounts, projections, and versions.
  - Example contents: node identity, trunk branches, exported capabilities.

2. Policy bundle
  - Declares ACLs, ownership, trust roots, read/write permissions, and mount rules.

3. Runtime cache
  - In-memory resolved namespace view used by the agent for fast lookup.

### 15.2 Recommended Storage Format

Use declarative, versioned documents for portability.

- YAML or JSON for source-controlled configuration
- Signed manifest for trust and integrity
- Optional compiled cache for fast startup

Suggested layout:

- /etc/agent/namespace.d/*.yaml for local config
- /var/lib/agent/namespace.cache for resolved runtime cache
- /var/lib/agent/namespace.signatures for trust material

For distributed deployment, the same data can also be published as streams:

- trunk/meta for identity and version
- trunk/coord for mounts and ownership
- trunk/control for namespace update commands

### 15.3 Configuration Model

Configuration should be declarative and layered.

Order of precedence:

1. Built-in defaults
2. Local node config
3. Signed deployment policy
4. Remote mounted namespace rules
5. Runtime overrides with explicit lease or admin rights

Configuration should describe:

- local trunk root
- mounted remote branches
- projection capabilities
- path versions
- policy inheritance
- event routing rules

### 15.4 Load Sequence

Namespace loading should be deterministic:

1. Load built-in defaults
2. Load local manifest files
3. Verify signatures and trust chain
4. Merge policy bundles
5. Resolve mounts
6. Build runtime namespace cache
7. Publish node/meta readiness

If a mount or policy fails validation, the agent should degrade gracefully and expose the unresolved state in node/health.

### 15.5 Update and Reload

Namespace changes should be applied through explicit control events.

- Validate new manifest or policy bundle
- Write update to a control stream or admin channel
- Rebuild resolved cache atomically
- Emit namespace changed event
- Keep old view available until new view is valid, if possible

Hot reload is allowed, but only with clear versioning and rollback support.

### 15.6 Distributed Namespace Sync

In a multi-node system, namespace data can be synchronized in three ways:

- Push: central deployment publishes updated manifests
- Pull: node fetches signed namespace policy on startup or refresh
- Peer mount: one node mounts another node’s exported namespace branch

The source of truth should be explicit per path prefix so different branches can have different owners.

### 15.7 Practical Rule

Use this rule of thumb:

- Store namespace definition in manifests.
- Configure namespace through signed policy and mounts.
- Load namespace into a runtime cache at agent startup.
- Expose the resolved namespace back out through trunk/meta and trunk/coord.
