# EvidenceRef Ledger Bridge Design M385

## Problem

Agent Operations preserves evidence through lightweight `NexaEvidenceRef` values across Mission, Task, Run Report, Progress Report, and Verification Before Done results. Those references are useful for compatibility but are not ledger-aware.

Before UI, orchestration, or persistence uses these references, NODAL OS needs a governed bridge that records evidence provenance and authority without turning evidence into permission.

## Design Decision

M383-M385 adds an adapter-style bridge:

- `NodalOsEvidenceBridgeRef`
- `NodalOsEvidenceBridgeResult`
- `NodalOsEvidenceBridgeOptions`
- `NodalOsEvidenceRefBridge`

The bridge does not replace `EvidenceLedger` or `BrowserPersistentAuditLedger`. It maps lightweight evidence refs into richer metadata suitable for later ledger integration.

## Source Kinds

The bridge classifies evidence source as:

- `AgentOperation`
- `Mission`
- `AgentTask`
- `RunReport`
- `ProgressReport`
- `VerificationGate`
- `RecipeManifest`
- `StepLibrary`
- `BrowserRuntime`
- `OcrObservation`
- `Manual`
- `Unknown`

`Unknown` is allowed only as diagnostic-quality evidence with warning.

## Use Kinds

Evidence use is classified as:

- `DiagnosticOnly`
- `Auxiliary`
- `VerificationSupport`
- `AuditTrail`

This prevents later consumers from treating every evidence reference as verification-grade or audit-grade by default.

## Authority Model

The authority model is intentionally narrow:

- `NoAuthority`
- `SupportsVerificationOnly`
- `DiagnosticOnly`

There is no action-authorizing authority. Evidence cannot approve, click, submit, send, delete, pay, sign, or unlock policy.

## Sensitivity And Redaction

The bridge records:

- `Unknown`
- `NonSensitive`
- `PotentiallySensitive`
- `Sensitive`
- `SecretRedacted`

Redaction state is recorded separately:

- `Unknown`
- `NotRequired`
- `Redacted`
- `RedactionRequired`
- `RejectedSensitive`

The bridge uses the common redaction service introduced in M380-M382. Sensitive references are redacted before being returned and are rejected by default unless already redacted or explicitly configured for design-phase acceptance.

## Relationship With EvidenceLedger

`EvidenceLedger` remains the active core ledger for FSM/transition evidence. The bridge does not write to it in this milestone.

Later work can attach a concrete ledger entry id through `LedgerRef` once the persistence boundary is defined.

## Relationship With BrowserPersistentAuditLedger

`BrowserPersistentAuditLedger` remains the active browser audit trail. The bridge recognizes audit-style refs and can preserve `LedgerRef`, but it does not append audit events or create persistence side effects.

## Relationship With OCR Evidence

The bridge follows the same boundary as OCR evidence:

- observational evidence can support verification,
- raw or sensitive evidence must be governed,
- evidence has no action authority.

OCR evidence remains in its existing path and is not rewritten.

## Relationship With Agent Operations

Bridge helpers cover:

- direct `NexaEvidenceRef`,
- `NexaRunReport`,
- `NodalOsAgentProgressReport`,
- `NodalOsVerificationBeforeDoneResult`.

This keeps Mission/Task/Run/Progress/Verification compatibility while adding richer bridge metadata for future persistence.

## What This Does Not Implement

- No UI.
- No orchestration API.
- No recipe execution.
- No scheduled runs.
- No browser or desktop action execution.
- No new persistence DB.
- No EvidenceLedger rewrite.
- No namespace move.
- No broad rename.

## Next Steps

Recommended next milestone:

`M386-M388 Recipe/Step Runtime Permission Wording Cleanup or Agent Operations Namespace ADR`
