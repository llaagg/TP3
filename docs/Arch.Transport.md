# Transport Design

## 1. Purpose

Define the transport layer for the agent-based distributed system.

The transport layer is responsible for moving messages and streams between agents, devices, and clients.

---

## 2. Transport Role

The transport layer should be:
- pluggable
- secure
- reliable enough for the target use case
- capable of supporting multiple modalities (control, state, events)

The transport layer is not the same as the namespace or the access model. It carries data; access controls whether the data can be used.

---

## 3. Transport Types

### 3.1 MQTT

Best fit for IoT and lightweight device connectivity.

Strengths:
- topic tree fits namespace-like structures
- pub/sub works naturally for events
- retained messages can represent durable state
- wide support on microcontrollers and mobile devices

Use MQTT for:
- event streams
- state updates
- lightweight control commands

### 3.2 TCP / gRPC

Best fit for higher-throughput or more structured RPC.

Strengths:
- strong request/response semantics
- streaming support with backpressure
- richer typing and schema support

Use TCP/gRPC for:
- compute invocation with complex payloads
- large state transfers
- custom binary streams

### 3.3 Local IPC

Best fit for local agent-to-app communication.

Examples:
- named pipes on Windows
- UNIX domain sockets on Linux/macOS
- loopback TCP for platform-agnostic local access

Use local IPC for:
- desktop apps talking to the local agent
- system services integrating with the agent

### 3.4 Bluetooth / USB / Serial

Best fit for direct connectivity to constrained devices.

Use these transports when a device cannot use TCP/MQTT directly and needs a local bridge.

---

## 4. Transport Abstraction

The agent should define a transport interface with these responsibilities:
- connect / disconnect
- send message / receive message
- open stream / close stream
- negotiate protocol version
- report health and status

Transport implementations should be replaceable without changing namespace or access logic.

---

## 5. Transport Semantics

Transport must support the following modes:
- message delivery (control commands, metadata)
- stream delivery (data, logs, events)
- request/response (compute invoke)
- publish/subscribe (events, state changes)

For each mode, the transport should provide the necessary reliability guarantees or allow the upper layer to handle retries and deduplication.

---

## 6. Security

Transport security is mandatory.

Recommended defaults:
- TLS/mTLS for TCP/gRPC
- TLS for MQTT
- authenticated local IPC (ACLs or OS identity)
- encryption for Bluetooth/USB/serial where possible

Transport must preserve identity and allow the access layer to verify the peer.

---

## 7. Transport Selection Guideline

Use this rule of thumb:
- MQTT for wide IoT compatibility and event/state paths
- TCP/gRPC for compute and heavy streams
- Local IPC for desktop/local integration
- Bluetooth/USB/Serial for constrained or embedded attachments

The agent should support multiple transports concurrently.

---

## 8. Example Transport Mapping

| Use case | Transport | Why |
|---|---|---|
| phone sensor events | MQTT | lightweight pub/sub, retained state |
| large file sync | TCP/gRPC | streaming and throughput |
| local desktop app | IPC | low latency, local-only |
| USB-connected MCU | Serial | direct physical link |

---

## 9. Delivery

Transport is the plumbing layer. The rest of the system should remain transport-agnostic.

The agent’s namespace manager and access layer should work the same regardless of whether a message arrived over MQTT, gRPC, or local IPC.
