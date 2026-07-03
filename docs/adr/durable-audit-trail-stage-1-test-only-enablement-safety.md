# Durable Audit Trail Stage 1 Test-Only Enablement Safety

## Status

Accepted as Stage 1 test-only hardening. Product enablement is not authorized.

## Baseline

- Input baseline: `b5327bbddbd75010ec7ec61546cb8d64e3ecc963`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Previous external audit: `GO_EXTERNAL_AUDIT_DURABLE_AUDIT_TRAIL_PRE_ENABLEMENT_CONTROL_PLANE_READY`
- Capability: `DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL`
- Current state after this block: Stage 1 test-only/local-temp hardening, implemented-not-enabled.

## Stage 1 Scope Lock

Stage 1 means test-only fixture use with an explicit local/temp storage root. It does not authorize Stage 2, dev sandbox runtime, product runtime, service registration, command handlers, UI product actions, product ledger paths, DB/migration, cloud/network/provider, Browser/CDP, WCU/OCR, recipes live writes, release readiness, or commercial readiness.

Allowed in Stage 1:

- local/temp JSONL ledger writes from focused tests;
- explicit fixture-created temp roots;
- local lock behavior;
- append-only invariant checks;
- malformed ledger, invalid shape, tamper and replay/read verification tests;
- secret-like rejection tests;
- static no-enable guard tests.

Prohibited in Stage 1:

- product ledger path;
- runtime service registration;
- command handler or command bus wiring;
- UI product action buttons;
- broad execution or domain mutation;
- physical export;
- redaction runtime enablement;
- retention/deletion runtime;
- DB/migration-backed audit trail;
- provider/cloud/network persistence;
- Browser/CDP, WCU/OCR or recipes live writes;
- production, release or commercial readiness claims.

## Implementation Hardening

The Stage 1 hardening keeps the candidate isolated and adds a verification boundary:

- `Append` remains gated by `DurableAuditTrailAppendOnlyMinimalPolicy.Enabled`.
- Storage root must remain inside the OS temp path when `AllowLocalTestStorageOnly` is true.
- `VerifyFile` now fails closed for ledger paths outside the local/temp boundary.
- Path boundary checking uses a normalized temp path with a trailing directory separator.
- Rejections keep `AppendWriteCount = 0` and `PersistedEventCount = 0`.
- Accepted test-only appends keep `ProductActionAllowed = false`, `NetworkAllowed = false`, `DbMigrationAllowed = false`, `CommandHandlerRegistered = false`, and `ReleaseCommercialReady = false`.

## Test Coverage Added Or Reinforced

- Stage 1 fixture uses only temp/local-test ledger paths.
- Outside-boundary append is rejected and does not create directories.
- Outside-boundary verification fails closed.
- Append-only behavior preserves existing JSONL lines and adds a new line.
- Repeated reads after append return the same head state.
- Concurrent local/test appends remain valid under the local lock.
- Sequence numbers remain monotonic after concurrent test-only appends.
- Existing malformed JSON, invalid shape, tamper, sequence, replay and hash mismatch tests remain active.
- Secret-like rejection corpus remains active and does not replace redaction-before-persistence.
- Static source guard verifies no runtime registration, service registration, command handler wiring, product actions, DB/migration provider, network provider, Browser/CDP, WCU/OCR or recipes execution path in the minimal candidate.

## Remaining Blockers

These items still block any product enablement:

- redaction-before-persistence runtime remains not implemented;
- product runtime feature flag remains not implemented;
- property/stress coverage is Stage 1 minimal, not product enablement evidence;
- product ledger path is not authorized;
- runtime verifier is not authorized;
- checkpoint writer is not authorized;
- service registration remains prohibited;
- command handlers remain prohibited;
- UI product actions remain prohibited;
- external audit is required before any later stage;
- explicit human GO is required before any later stage;
- release/commercial readiness remains NO-GO.

## Percentages

- Durable audit trail local/test-safe append/write candidate: `92-95%`
- Stage 1 test-only enablement safety: `85-90%`
- Product enablement: `0%`
- Runtime/live: `0%`
- Execution/mutation broad: `0%`
- Release/commercial readiness: `0% / NO-GO`
- Project usable end-to-end estimate: `20-30%`

## Mega-Audit Addendum (Stage 1 Controlled Fixes)

A subsequent Claude mega-audit of the full Durable Audit Trail Stage 1 line applied
controlled test-only/local-safe fixes without changing scope:

- Added a `MalformedMetadata` reject reason and null-total write-side validation so a null
  reference field, a null evidence element, a null evidence list, a null metadata value or
  a blank metadata key now fail closed cleanly instead of throwing
  `NullReferenceException`. This also prevents a null metadata value from being persisted
  and later poisoning the ledger through the read-side shape check.
- `ContainsSecretLikeContent` and the secret scan over collections are now null-safe.
- Added a Safety test proving these malformed inputs fail closed with no side effects and
  no ledger file creation (focused Safety count 15 → 16).

Known remnant risks recorded for the external audit / Stage 2 planning, not changed here:

- `AllowLocalTestStorageOnly = false` lets `Append` write outside the temp boundary while
  `VerifyFile` always enforces it. This flag is an intentional future-approved-caller seam
  and defaults to `true` (temp-only). A future caller could create a ledger it cannot later
  verify with `VerifyFile`.
- `LedgerLocks` is a process-static map keyed by full path and does not evict entries;
  negligible in Stage 1 test-only use.

This addendum does not enable product runtime behavior.

## Decision

`GO_DURABLE_AUDIT_TRAIL_STAGE_1_TEST_ONLY_ENABLEMENT_SAFETY_READY`

This ADR does not enable product runtime behavior.
