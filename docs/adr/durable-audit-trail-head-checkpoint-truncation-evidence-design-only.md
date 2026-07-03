# Durable Audit Trail Head Checkpoint / Truncation Evidence Design-Only

## Status

Accepted as design-only. Not implemented.

## Context

`DURABLE_AUDIT_TRAIL_APPEND_ONLY_MINIMAL` is an isolated local/test-safe JSONL ledger for `approval.reviewed` events. It currently verifies the internal ledger hash chain before append and rejects malformed, semantically invalid, reordered, duplicated, or hash-mutated entries.

Round 2 deliberately deferred a separate head-checkpoint layer. Without a checkpoint outside the ledger's internal chain, a valid latest-line deletion, rollback to an older valid ledger, or replacement by a previous internally consistent ledger cannot be detected by the minimal ledger alone.

## Decision

Define `DURABLE_AUDIT_TRAIL_HEAD_CHECKPOINT_TRUNCATION_EVIDENCE_DESIGN_ONLY` as the next local/test-safe design layer. This ADR defines the future checkpoint model, verification model, trust boundaries, reason codes, and integration gates. It does not write checkpoints, verify checkpoints at runtime, register services, expose command handlers, execute product actions, add storage migrations, or claim production audit readiness.

## Non-Goals

- No active checkpoint writer.
- No active checkpoint verifier.
- No runtime integration.
- No service registration.
- No command handler.
- No product action.
- No approval execution.
- No approval mutation store.
- No DB or migration.
- No network, provider, cloud, KMS, or external trust boundary implementation.
- No legal signature or cryptographic signature execution beyond the existing local SHA-256 hash-chain concept.
- No WORM, compliance-grade, production audit trail, release-ready, or commercial-ready claim.

## Threat Model

### Internally Detectable By The Current Ledger

- Event mutation that changes the hashed record.
- `previousHash` mismatch.
- `eventHash` mismatch.
- Sequence gap.
- Duplicate sequence.
- Reordered records.
- Invalid JSON line.
- Invalid entry shape.
- Empty or whitespace-only ledger line.

### Not Detectable Without A Checkpoint

- Deleting the latest valid ledger line.
- Truncating the ledger to an older internally consistent head.
- Replacing the ledger with an older valid copy.
- Replacing the ledger with a different valid ledger that still has a consistent internal hash chain.

### Outside Current Scope

- Attacker with total control of the filesystem.
- Replacing both ledger and checkpoint inside the same trust boundary.
- Rollback of the full containing directory.
- Clock tampering.
- Process compromise.
- Local administrator or malware control.
- Distributed adversary model.
- Compliance, WORM, legal retention, or production audit guarantees.

## Conceptual Checkpoint Model

Future model name: `LedgerHeadCheckpoint`.

Required conceptual fields:

- `CapabilityId`: fixed to the durable audit trail checkpoint capability.
- `LedgerId`: stable local/test-safe ledger identifier, not a secret.
- `LedgerPathFingerprint`: safe path fingerprint, not a raw sensitive absolute path.
- `LedgerFileName`: expected ledger file name.
- `HeadSequence`: sequence number of the ledger head at checkpoint time.
- `HeadEventHash`: event hash of the ledger head.
- `HeadPreviousHash`: previous hash stored by the ledger head.
- `PreviousCheckpointHash`: previous checkpoint hash when a checkpoint chain is used.
- `HeadCheckpointHash`: hash of the checkpoint envelope.
- `CreatedAtUtc`: checkpoint creation time.
- `CheckpointReason`: reason such as `after_append`, `after_human_review`, or `before_future_export`.
- `CheckpointMode`: `local_file`, `local_chain`, or future design-only levels.
- `CheckpointVersion`: versioned shape discriminator.
- `SafeSummaryOnly`: true.
- `IsLocalTestSafeOnly`: true.
- `IsProductionReady`: false.
- `IsWormBacked`: false.
- `IsComplianceGrade`: false.

The checkpoint must not include raw payloads, secrets, approval content payloads, product action payloads, external credentials, auth material, or sensitive absolute paths.

## Future Verification Model

Inputs:

- Current ledger file.
- Expected checkpoint.
- Optional checkpoint chain.

Outputs:

- `PASS`, `WARNING`, or `FAIL`.
- Structured reason codes.
- Safe summary only.

Future verification cases:

- Ledger head matches checkpoint.
- Ledger has additional entries after checkpoint.
- Ledger is behind checkpoint.
- Ledger head sequence is lower than checkpoint sequence.
- Ledger head hash differs from checkpoint hash.
- Ledger is empty but checkpoint says a prior head exists.
- Ledger is internally valid but does not match the checkpoint.
- Checkpoint is missing.
- Checkpoint is corrupt or unsupported.
- Multiple checkpoints exist.
- Checkpoint chain mismatch.
- Ledger and checkpoint are both replaced inside the same trust boundary.

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

Level 0: no checkpoint. This is the current state.

Level 1: local checkpoint file. This can provide evidence of tail deletion if the checkpoint is not replaced with the ledger.

Level 2: local checkpoint chain. This can improve evidence for partial rollback and checkpoint continuity, still inside a local/test-safe boundary.

Level 3: external checkpoint boundary. Future design only; not implemented here.

Level 4: KMS, WORM, compliance, or legal-retention backed checkpoint. Out of scope.

## Future Approval Integration Gates

A future approval integration gate must require:

- Current ledger verification is valid.
- Expected checkpoint verification is valid.
- No rollback or tail deletion is suspected.
- Checkpoint shape is safe and supported.
- No unsafe payload, secret-like content, or missing evidence reference is present.

The gate must continue to block:

- Product action execution.
- Runtime operation.
- Command handler exposure.
- Approval mutation store writes.
- DB or migration dependency.
- Network/provider/cloud calls.

## Consequences

This design clarifies the distinction between internal hash-chain tamper detection and checkpoint-assisted rollback evidence. It also prevents overclaiming: a local checkpoint does not make the ledger WORM-backed, compliance-grade, production-ready, or impossible to tamper with.

## Next Safe Step

`NODAL_OS_DURABLE_AUDIT_TRAIL_HEAD_CHECKPOINT_LOCAL_TEST_SAFE_IMPLEMENTATION`, only after another explicit implementation block and while preserving local/test-safe boundaries.
