# Simple Architecture Design

## 1. Purpose

Capture the minimal agreed architecture for the agent-based distributed system.

Focus on only the decided concepts:
- trunk as the opinionated root
- namespace as the flexible composition layer
- streams + compute as the core runtime abstraction
- agent as transport/access/namespace manager

---

## 2. Core Concepts

### 2.1 Trunk

Trunk is the opinionated anchor for each node.

It exposes the canonical access points:
- `/nodes/{node_id}/trunk/meta`
- `/nodes/{node_id}/trunk/control/*`
- `/nodes/{node_id}/trunk/state/*`
- `/nodes/{node_id}/trunk/events/*`
- `/nodes/{node_id}/trunk/compute/*`

Trunk is stable, predictable, and the main entry point for node-level resources.

### 2.2 Namespace

Namespace is the flexible layer on top of trunk.

It allows:
- mounting remote resources
- merging views from multiple devices
- creating app-specific collections
- exposing namespaces like `/namespaces/photos-all`

Namespace can be custom and composable without changing trunk.

### 2.3 Streams + Compute

The runtime abstraction is:
- streams for data and events
- compute for actions and commands

This is enough to cover:
- state as durable streams
- control as write streams or compute invocations
- events as unbounded event streams

Coordination is still required, but it should be applied through metadata and policies, not as a separate runtime concept.

---

## 3. Agent Structure

The agent has three main layers:

1. Transport
   - handles network and local communication
   - examples: MQTT, TCP, local IPC, USB, Bluetooth

2. Access
   - handles authentication and authorization
   - enforces policy and identity

3. Namespace manager
   - resolves trunks and mounted namespaces
   - merges views and exposes the logical map to clients

Apps talk only to the agent, not directly to each device.

---

## 4. Simple Example

Agent view:

- `/nodes/laptop-01/trunk/meta`
- `/nodes/laptop-01/trunk/state/system`
- `/nodes/phone-01/trunk/state/photos`
- `/nodes/camera-02/trunk/state/photos`
- `/nodes/phone-01/trunk/events/notifications`

Custom namespaces:

- `/namespaces/photos-all/` = union of phone and camera photo streams
- `/namespaces/office/` = merged cloud docs and laptop documents
- `/namespaces/phone-events/` = phone notification event stream

This is the practical design: trunk is the stable root, namespaces are the flexible user-facing layer.

---

## 5. How Apps Use It

Apps request a namespace from the agent, for example:
- `GET /namespaces/photos-all`
- `GET /nodes/phone-01/trunk/events/notifications`

The agent returns a resolved view or a stream handle.

Apps do not need to know where resources live; they only need to know the namespace path.

---

## 6. Configuration and Loading

Keep namespace config simple:
- store namespace manifests in YAML or JSON
- load them at startup
- validate mounts and policies
- expose resolved namespace through `trunk/meta`

This is enough for a minimal working design without extra complexity.
