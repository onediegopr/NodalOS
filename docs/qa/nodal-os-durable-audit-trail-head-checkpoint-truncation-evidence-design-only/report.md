# NODAL OS - Durable Audit Trail Head Checkpoint / Truncation Evidence Design-Only

## Decision

GO_HEAD_CHECKPOINT_TRUNCATION_EVIDENCE_DESIGN_ONLY

## Capability

- Existing capability: `DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL`
- New design-only capability: `DURABLE_AUDIT_TRAIL_HEAD_CHECKPOINT_TRUNCATION_EVIDENCE_DESIGN_ONLY`

## Scope

This block designs the next evidence layer for valid-tail deletion, rollback to an older valid ledger, and internally valid ledger replacement. It does not implement checkpoint writing, checkpoint verification, runtime integration, service registration, command handlers, product actions, DB/migration, network/provider/cloud, KMS, signatures, WORM backing, or production audit readiness.

## Current State Inventory

The current minimal ledger detects:

- malformed JSON;
- invalid entry shape;
- empty or whitespace-only JSONL ledger lines;
- sequence gap;
- duplicate sequence;
- reordered records;
- previous-hash mismatch;
- event-hash mismatch;
- semantic event mutation covered by hash mismatch;
- append after existing-ledger corruption.

The current minimal ledger does not detect:

- deletion of the latest valid line;
- truncation to an older internally consistent head;
- replacement with an older valid ledger;
- replacement of ledger plus checkpoint inside the same future trust boundary;
- crash-safe transactional append;
- cross-process or distributed writer coordination;
- WORM/compliance-grade retention.

## Threat Model

### Internally Detectable

Internal hash-chain verification can detect mutation, missing sequence continuity, duplicate sequence, reorder, invalid JSON, invalid shape, previous-hash mismatch, and event-hash mismatch.

### Requires A Checkpoint

Tail deletion and rollback to an older valid ledger require a checkpoint or other head evidence outside the ledger's internal chain.

### Out Of Scope

An attacker who can replace the ledger and checkpoint together in the same trust boundary remains out of scope. KMS, cloud boundary, legal signature, WORM, compliance-grade storage, production audit trail, and runtime integration are not implemented or claimed.

## Checkpoint Model

Future conceptual model: `LedgerHeadCheckpoint`.

Fields:

- `CapabilityId`
- `LedgerId`
- `LedgerPathFingerprint`
- `LedgerFileName`
- `HeadSequence`
- `HeadEventHash`
- `HeadPreviousHash`
- `PreviousCheckpointHash`
- `HeadCheckpointHash`
- `CreatedAtUtc`
- `CheckpointReason`
- `CheckpointMode`
- `CheckpointVersion`
- `SafeSummaryOnly`
- `IsLocalTestSafeOnly`
- `IsProductionReady=false`
- `IsWormBacked=false`
- `IsComplianceGrade=false`

Forbidden fields/content:

- raw payload;
- secrets;
- approval content payloads;
- product action payloads;
- credentials;
- cookies/tokens/auth material;
- sensitive absolute paths.

## Verification Model

Future inputs:

- current ledger file;
- expected checkpoint;
- optional checkpoint chain.

Future outputs:

- `PASS`, `WARNING`, or `FAIL`;
- structured reasons;
- safe summary only.

Future cases:

- ledger head matches checkpoint;
- ledger has entries after checkpoint;
- ledger is behind checkpoint;
- head sequence is lower than checkpoint;
- head hash differs from checkpoint;
- empty ledger with non-empty checkpoint;
- internally valid ledger mismatches checkpoint;
- missing checkpoint;
- corrupt checkpoint;
- unsupported checkpoint version;
- multiple checkpoints;
- checkpoint chain mismatch;
- ledger and checkpoint both replaced inside the same trust boundary.

Future reason codes:

- `HeadCheckpointMissing`
- `HeadCheckpointMismatch`
- `LedgerBehindCheckpoint`
- `LedgerRollbackSuspected`
- `LedgerTailDeletionSuspected`
- `CheckpointChainMismatch`
- `CheckpointUnsupportedVersion`
- `CheckpointUnsafeShape`
- `CheckpointNotProductionTrustBoundary`
- `CheckpointVerificationNotImplemented`

## Trust Boundary Levels

- Level 0: no checkpoint; current state.
- Level 1: local checkpoint file; local/test-safe evidence only.
- Level 2: local checkpoint chain; stronger local continuity evidence.
- Level 3: external checkpoint boundary; future design, not implemented.
- Level 4: KMS/WORM/compliance-grade boundary; out of scope.

## Future Approval Gates

Future approval integration must require:

- valid ledger;
- valid checkpoint;
- no rollback suspected;
- no tail deletion suspected;
- supported safe checkpoint shape;
- safe-summary-only evidence;
- no unsafe payload or secret-like material.

Future approval integration remains blocked from product action execution, runtime operation, command handler exposure, approval mutation store writes, DB/migration, network/provider/cloud calls, or release/commercial readiness.

## Anti-Overclaim

- No WORM claim.
- No immutable guarantee.
- No tamper-proof claim.
- No compliance-grade claim.
- No production audit trail claim.
- No legal signature claim.
- No KMS/signature implementation.
- No runtime/product exposure readiness claim.

## Tests

No new executable tests were added in this block. The output is design-only documentation and ADR. Existing clean-closure validation remains required for the block.

## Next Safe Step

`NODAL_OS_DURABLE_AUDIT_TRAIL_HEAD_CHECKPOINT_LOCAL_TEST_SAFE_IMPLEMENTATION`

That next step should remain local/test-safe and must not open runtime/product integration.
