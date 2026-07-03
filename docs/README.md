# TP3 Usage Flow

This document extracts the practical usage sequence from the integration test in `src/TP3.Tests/FlowTest.cs`.

## Core Session Flow

The server interaction is stateful and tag-based. A client typically follows this sequence:

1. `ATTACH` a session using a tag. This is done once per session.
2. `WALK` to the desired node.
3. `OPEN` the node.
4. `READ` data from the opened node.
5. `CLUNK` when finished.

## Directory Listing Flow

To list a directory:

1. Send `ATTACH` once per session.
2. Send `OPEN` for the attached tag.
3. Send repeated `READ` requests with increasing `offset` until the server returns no bytes or fewer bytes than requested.
4. Deserialize the returned read stream into `TP3StatPayload` entries.
5. Send `CLUNK`.

The test uses `TP3StatPayloadExtensions.Deserilize(Stream)` to decode concatenated JSON payloads from the directory stream.

## File Reading Flow

To read a file:

1. `WALK` to the file name.
2. `OPEN` the file.
3. `READ` the file contents.
4. Treat the returned bytes as payload data.

## Example Sequence

The integration test exercises the following path:

1. `ATTACH` the root tag.
2. `OPEN` the root.
3. `READ` the directory stream.
4. `CLUNK`.
5. `WALK` into a child directory.
6. `OPEN` again.
7. `READ` again.
8. `CLUNK`.
9. `WALK` to `README.md`.
10. `OPEN` the file.
11. `READ` the file bytes.

## Notes

- `WALK` returns the resolved node chain, not a string path.
- `READ` on directories returns concatenated `TP3StatPayload` JSON objects.
- `READ` on files returns raw file bytes.
- `CLUNK` releases the current tag/session binding.
