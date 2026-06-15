# ADR M50: Audit Integrity Key Custody

## Status

Accepted for M50.

## Context

External audit found C-1: the audit ledger HMAC/head seal existed mechanically, but `BrowserPersistentAuditLedger` silently fell back to a development fixture HMAC key when no integrity provider was injected. That created false security because an operator could believe the ledger was sealed with managed key custody while it was actually using a hardcoded dev key.

## Decision

M50 removes the automatic fallback. `BrowserPersistentAuditLedger` now fails closed unless an explicit `IBrowserAuditLedgerIntegrityProvider` is supplied and healthy.

Supported provider kinds are:

- `Disabled`;
- `DevFixtureExplicit`;
- `OsBackedDpapiCurrentUser`;
- `ExternalFuture`.

`DevFixtureExplicit` is only acceptable for development and test fixtures. It is prohibited for `ProductionLocked` and `EnterpriseControlled` profiles.

## OS-backed Provider

The minimal OS-backed provider is `OsBackedDpapiCurrentUser`. On Windows it creates or loads a local HMAC key, protects the stored key material with DPAPI CurrentUser, associates it with `KeyId` and `KeyVersion`, and exposes only key metadata and health status.

If DPAPI is unavailable, the provider reports unavailability by throwing `PlatformNotSupportedException`; it never falls back to the dev fixture key.

## Head Seal

The head seal now includes:

- key provider kind;
- key id;
- key version;
- integrity algorithm;
- event count;
- last sequence;
- last event hash;
- HMAC;
- timestamps.

Verification fails on key id mismatch, key version mismatch, provider mismatch, algorithm mismatch, unhealthy key provider, event count mismatch, last sequence mismatch, last hash mismatch, tamper, or truncation.

## Rotation

M50 models key rotation with requested/planned/blocked/completed states, previous key id, new key id, and redacted audit reason. It does not implement full production rotation yet.

## Guarantees

M50 guarantees default fail-closed behavior, explicit key provider requirement, no raw key in public DTOs, OS-backed local key custody for private/local preview, and head-seal correlation to key metadata.

## Non-Guarantees

DPAPI CurrentUser does not protect against malware running as the same Windows user. It is not an HSM, not an external enterprise vault, not portable without a migration plan, and not sufficient alone for legal/compliance-grade evidence.

## Future Work

Before legal/compliance audit evidence, NEXA still needs production key rotation, backup/recovery, external custody option, operator separation, formal key access audit, and compliance review.
