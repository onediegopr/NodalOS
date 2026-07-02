# NODAL OS - Redaction / Retention / Deletion Deepening Design-Only Protected

## Decision

NODAL OS accepts a protected, design-only contract for future redaction, retention and deletion policy requirements in Approval/Human Review, physical export and durable audit trail contexts.

Decision target:

`GO_NODAL_OS_REDACTION_RETENTION_DELETION_DEEPENING_DESIGN_ONLY_PROTECTED_READY`

Control phrase:

`Redaction/retention/deletion design may increase. Runtime, storage, export and implementation readiness remain 0%.`

## Context

The controlled execution and physical export design tracks are closed and paused at:

`a08273d49c72168d297c8ec717894e09c3eb1383`

The remaining protected debt includes redaction runtime design, retention/deletion policy design, destination policy, durable audit trail implementation, mutation store implementation, writer/policy productive implementation, runtime gate implementation, physical export implementation and external audit before any implementation.

This ADR deepens only the redaction/retention/deletion policy design. It does not implement runtime behavior.

## What Was Designed

- `RedactionRetentionDeletionPolicyDesignOnlyProtected`
- `RedactionRetentionDeletionReadiness`
- `RedactionRetentionDeletionCapabilityStatus`
- `RedactionPolicyPreview`
- `SecretPiiScanRequirement`
- `RetentionPolicyPreview`
- `DeletionPolicyPreview`
- `TombstonePolicyPreview`
- `LegalHoldPolicyPreview`
- `EvidenceLinkageRequirement`
- `ExportLinkageRequirement`
- `AuditTrailLinkageRequirement`
- `RedactionRetentionDeletionBlockedReason`
- `RedactionRetentionDeletionAntiCapabilityProof`
- `RedactionRetentionDeletionFutureImplementationChecklist`

The model is deterministic, read-only and preview-only. It expresses future blockers and safety proof, not runtime capabilities.

## Current Baseline

- Durable approval audit trail design already models redaction, retention and deletion requirements as future-only with implementation readiness at `0%`.
- Physical export policy design blocks every format until redaction runtime, retention/deletion policy and durable audit trail implementation exist.
- Human review evidence link guards exclude raw payload and secret-like evidence from read-only packet linkage.
- No redaction runtime, secret scanner, PII scanner, retention store, deletion workflow, tombstone writer or legal hold store exists in this hito.

## Non-Goals

- No redaction runtime.
- No secret scan.
- No PII scan.
- No retention store.
- No retention workflow.
- No deletion workflow.
- No tombstone write.
- No legal hold store.
- No filesystem IO.
- No DB.
- No durable audit trail real.
- No physical export.
- No approval execution.
- No approval mutation.
- No runtime/live.
- No writer/policy productive integration.
- No service registration.
- No command handlers.
- No provider/cloud/network.
- No LLM/vector/memory.
- No browser/CDP/WCU/OCR.
- No recipe execution.
- No release/commercial readiness.

## Readiness

- Redaction runtime readiness: `0%`.
- Secret scan readiness: `0%`.
- PII scan readiness: `0%`.
- Retention store readiness: `0%`.
- Retention workflow readiness: `0%`.
- Deletion workflow readiness: `0%`.
- Tombstone writer readiness: `0%`.
- Legal hold store readiness: `0%`.
- Filesystem readiness: `0%`.
- DB readiness: `0%`.
- Physical export readiness: `0%`.
- Runtime/live readiness: `0%`.
- Release/commercial readiness: `NO-GO`.

## Required Future Blockers

- External audit of redaction policy and secret/PII requirements.
- Explicit approval before any runtime implementation.
- Evidence-ref-only audit contract before raw payload handling.
- Retention class policy with workspace and user consent boundaries.
- Deletion eligibility policy with tombstone and legal hold governance.
- Durable audit trail implementation approval.
- Database/filesystem policy approval and external audit.
- Physical export policy external audit before any export implementation.

## Safety Proof

The fixture asserts:

- Redaction action count: `0`.
- Retention action count: `0`.
- Deletion action count: `0`.
- Tombstone count: `0`.
- Legal hold action count: `0`.
- Export action count: `0`.
- Product action count: `0`.
- All productive capability flags are `false`.
- All anti-capabilities are `true`.

## Consequences

This hito improves design clarity around privacy governance, but it does not increase runtime, storage, export, execution, mutation or release readiness.

Next safe step:

`NODAL_OS_REDACTION_RETENTION_DELETION_EXTERNAL_AUDIT`
