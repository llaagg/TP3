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

TP3 protocol is conceptually similar to 9P: client navigates a resource tree and then operates on resolved resource handles.

## Goals

* Keep messages small and explicit.
* Resolve paths once (`walk`), then use stable handle (`qid`) for data operations.
* Support file-sized and unbounded streams with one `read` model.

## Common Message Envelope

Each request/response frame should contain:

* `type` - message type, for example `Twalk`, `Rwalk`, `Tread`, `RreadChunk`, `Rerror`.
* `tag` - request correlation id (chosen by client, echoed by server).
* `payload` - operation-specific body.

`tag` allows multiple in-flight requests on one transport.

MVP decision:

* `tag` is required.
* `sessionId` is optional extension and is not used in current MVP.
* Session context is currently bound to the transport connection.

## Resource Identity

Server returns `qid` for resolved resources.

Suggested `qid` shape:

* `type` - `directory | file | stream | device | state`
* `id` - stable resource id (opaque for client)

## 1) Walk

Client sends path, server walks resource tree and returns success with name/type/qid.

### Request: `Twalk`

Payload:

* `pathSegments: string[]` - absolute or session-root-relative path split into segments.
* `createIfMissing: bool` - optional, default `false`.

Example:

```json
{
      "type": "Twalk",
      "tag": 42,
      "payload": {
            "pathSegments": ["plant", "line1", "motorA", "status"]
      }
}
```

### Success Response: `Rwalk`

Payload:

* `qid` - resolved resource handle.
* `name` - resolved node name.
* `resourceType` - canonical type.
* `meta` - optional metadata map.

Example:

```json
{
      "type": "Rwalk",
      "tag": 42,
      "payload": {
            "qid": { "type": "state", "id": "8f1f" },
            "name": "status",
            "resourceType": "state",
            "meta": { "contentType": "application/json" }
      }
}
```

### Error Response: `Rerror`

Recommended error codes for `walk`:

* `NotFound`
* `AccessDenied`
* `InvalidPath`
* `Conflict`

## 2) Read (stream)

Client requests file/resource content by `qid`. Server responds as stream.

Chunking policy belongs to the service implementation:

* Transport only carries frames and keeps ordering.
* Service decides if response is single frame or chunked stream.
* Directory listing is also a stream and may be chunked the same as file content.

### Request: `Tread`

Payload:

* `qid` - resource handle from `Rwalk`.
* `offset: long` - start offset, default `0`.
* `maxBytes: int` - preferred chunk/window size.
* `subscribe: bool` - if `true`, keep stream open for tail/live mode.

Example:

```json
{
      "type": "Tread",
      "tag": 43,
      "payload": {
            "qid": { "type": "file", "id": "ab91" },
            "offset": 0,
            "maxBytes": 65536,
            "subscribe": false
      }
}
```

### Stream Responses

Server uses 3 response frame types:

1. `RreadStart`
2. `RreadChunk` (0..N times)
3. `RreadEnd`

`RreadStart` payload:

* `qid`
* `contentType`
* `length` - optional total length if known.

`RreadChunk` payload:

* `qid`
* `sequence` - starts at 0, increments by 1.
* `offset`
* `data` - binary bytes (or base64 in JSON transport).
* `isLast`

`RreadEnd` payload:

* `qid`
* `totalBytes`
* `status: ok | cancelled | truncated`

### Error Response: `Rerror`

Recommended error codes for `read`:

* `BadQid`
* `NotReadable`
* `OffsetOutOfRange`
* `Timeout`
* `TransportClosed`

## Ordering and Concurrency Rules

* Frames with different `tag` may interleave.
* Frames for one `tag` must preserve order.
* `Rerror` terminates that request/tag.
* Client may cancel by sending `Tcancel { tag }`.

## Mapping to Current Code

Current model already has `Path` and `Payload` in `TP3Message` and `READ` command.

Suggested near-term evolution:

* Add `WALK` command.
* Add `Tag` and `Qid` fields to `TP3Message`.
* Keep `SessionId` as optional future extension (auth/reconnect scenarios).
* Implement read as chunked payload stream (`RreadStart/Chunk/End`) over existing transport.
