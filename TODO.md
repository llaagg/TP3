# TODO

## Namespace

- Define a global TP3 namespace model that can represent:
  - local device nodes
  - remote device nodes
  - service/control nodes
- Add stable namespace conventions:
  - `/devices/{deviceId}/...`
  - `/users/{userId}/...`
  - `/services/{serviceId}/...`
- Define node ownership and visibility rules (public, private, shared).
- Define cross-device discovery and attach semantics for namespace roots.
- Document how virtual nodes map to physical resources per platform (PC, Android/Android TV, iOS).

## Metadata Layer

- Define a dedicated metadata layer for node identity, ownership, capabilities, and lifecycle.
- Standardize metadata schema for all node types:
  - identity: `nodeId`, `parentId`, `path`, `version`
  - ownership: `owner`, `tenant`, `visibility`
  - capability flags: `read`, `write`, `execute`, `attach`, `share`
  - sync fields: `createdAt`, `updatedAt`, `etag`, `lastSeen`
- Separate data plane vs metadata plane operations.
- Add metadata discovery endpoint/path (for example `/meta` and per-node metadata files).
- Define metadata merge rules for cross-device conflicts (last-write-wins vs vector-clock style).
- Add metadata cache and invalidation strategy for offline/mobile devices.
- Add metadata permission checks tied to identity claims and scopes.
- Add tests for metadata consistency across:
  - local filesystem-backed nodes
  - remote/device-backed nodes
  - command/control nodes

## Authentication and Identity (Google Device Flow)

- Implement authentication using Google OAuth 2.0 Device Authorization Grant (Device Flow).
- Add a login flow that can run in CLI/headless scenarios.
- Exchange device code and verify tokens before issuing TP3 session permissions.
- Allow device-to-device attach when caller presents a valid Google token.
- Bind authenticated identity from the token to TP3 session/tag context.
- Define token validation and refresh strategy.
- Define logout/revocation behavior.

## Device-to-Device Attach Using Token

- Define attach contract: source device presents bearer token during `ATTACH` to target device.
- Validate token issuer, audience, expiry, and signature on target device.
- Map token subject to TP3 identity and authorization scope.
- Create short-lived TP3 session after successful validation.
- Reject attach on invalid token, wrong audience, expired token, or insufficient scope.
- Add replay protection (nonce/challenge or one-time attach token exchange).
- Add tests for:
  - valid token attach success
  - invalid token attach failure
  - expired token attach failure
  - cross-device attach with proper scope only

## Identity Proof in TP3

- Introduce identity claims in session context (subject, issuer, expiry, scopes).
- Enforce authorization checks on `ATTACH`, `WALK`, `OPEN`, `READ`, and `WRITE`.
- Add audit events for authentication success/failure and privileged operations.
- Add tests for:
  - unauthenticated access denial
  - authenticated namespace access
  - cross-device identity continuity

## Security and Platform Work

- Add TLS to the transport/control layer (TCO) for all remote TP3 connections.
- Define TLS profile:
  - TLS 1.2+ minimum (prefer TLS 1.3)
  - modern cipher suites only
  - certificate validation with hostname verification
- Support mutual TLS (mTLS) for device-to-device and device-to-service trust where needed.
- Add certificate lifecycle handling:
  - provisioning/bootstrap
  - rotation
  - revocation
- Ensure auth tokens/identity claims are only transmitted over TLS-protected channels.
- Add integration tests for:
  - handshake success/failure
  - invalid/expired certificate rejection
  - downgrade and plaintext fallback prevention

- Store secrets and tokens securely per platform (Windows, Android, iOS).
- Define minimum scope model for device, user, and service operations.
- Add rate limiting and brute-force protections for auth endpoints.
- Document threat model for user-space deployment across multiple device types.
