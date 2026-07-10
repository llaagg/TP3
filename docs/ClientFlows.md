# TP3 Client Flows

This document describes the practical client-side data flows currently used in TP3.

## Common Session Steps

All flows start from the same session lifecycle:

1. `ATTACH(tag)`
2. `WALK(tag, newTag, path...)`
3. `OPEN(tag)`
4. one or more `READ`/`WRITE` operations
5. `CLUNK(tag)`

## Flow 1: File (Read/Write)

Use this flow for regular file-like nodes.

1. `WRITE(tag, offset, data)` to update file bytes.
2. `READ(tag, offset, maxBytes)` to fetch file bytes.

Notes:

- Reads are offset-based and can be paged.
- Writes return byte count written.

## Flow 2: Directory (Read)

Use this flow for directory nodes.

1. `READ(tag, offset, maxBytes)` to fetch directory listing payload.
2. Repeat reads with increasing `offset` until EOF.

Notes:

- Directory listing is returned as serialized `TP3StatPayload` data.
- Directory nodes are read-only in current behavior.

## Flow 3: Command (Write Then Read)

Use this flow for command-like nodes.

1. `WRITE(tag, offset, inputBytes)` sends command input (arguments/payload).
2. `READ(tag, offset, maxBytes)` returns command output bytes.

Notes:

- Command output is read as regular stream bytes.
- This mirrors p9-style file interaction where command output is retrieved via `READ`, not a special command response.

## Walk Rule (p9-like)

Walking to a file as the final path segment is valid.

Walking through a file is invalid:

- valid: `/service/file`
- invalid: `/service/file/child`

If an intermediate path segment is a file, the walk should fail with a not-a-directory style error.
