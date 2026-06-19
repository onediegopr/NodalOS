# EvidenceRef Ledger Bridge Audit M383

## Scope

M383 audits the current NODAL OS evidence surfaces before adding an Agent Operations bridge from lightweight `NexaEvidenceRef` values to ledger-aware evidence metadata.

This audit is discovery-only. It does not replace the evidence ledger, change runtime behavior, or introduce persistence.

## Discovery Commands

`rg` was not available in the shell, so discovery used PowerShell `Select-String` over `src`, `tests`, `docs`, and `artifacts` for:

- `EvidenceLedger`
- `BrowserPersistentAuditLedger`
- `EvidenceRef`
- `NexaEvidenceRef`
- `OcrEvidence`
- `EvidenceSource`
- `LedgerEntry`
- `AuditLedger`
- `AuditTrail`
- `Authority`
- `NoAuthority`
- `Auxiliary`
- `Diagnostic`
- `Verification`
- `Provenance`
- `Redaction`
- `Sensitivity`
- `Sensitive`
- `RawImagePersisted`
- `CanAuthorizeAction`
- `CanApprove`
- `ActionAllowed`

## Evidence Models And Services Found

| Path | Role | Current classification |
| --- | --- | --- |
| `src/OneBrain.Core/Execution/EvidenceLedger.cs` | Core FSM/step transition evidence ledger. | Active evidence path |
| `src/OneBrain.BrowserExecutor.Contracts/BrowserPersistentAuditLedgerContracts.cs` | Browser audit ledger contracts. | Active audit path |
| `src/OneBrain.BrowserExecutor.Cdp/BrowserPersistentAuditLedger.cs` | Persistent browser audit ledger with redaction and integrity metadata. | Active audit path |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsOcrEvidenceLedgerContracts.cs` | OCR evidence ledger contracts. | Active OCR evidence path |
| `src/OneBrain.BrowserExecutor.Cdp/NodalOsOcrEvidenceIntegrationServices.cs` | OCR evidence integration and audit bridge. | Active OCR evidence adapter |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsAgentWorkboardContracts.cs` | Defines lightweight `NexaEvidenceRef`. | Agent Operations lightweight reference |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsRunReportContracts.cs` | Run report evidence refs on report, steps, and failures. | Agent Operations evidence consumer |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsAgentProgressReportingContracts.cs` | Progress report evidence refs on report, notes, blockers, decisions, and verification summaries. | Agent Operations evidence consumer |
| `src/OneBrain.BrowserExecutor.Contracts/NodalOsVerificationBeforeDoneContracts.cs` | Verification gate evidence refs. | Agent Operations evidence consumer |

## Evidence Active Path

The active evidence path has three separate but compatible layers:

- `EvidenceLedger` records execution/transition evidence for core FSM semantics.
- `BrowserPersistentAuditLedger` records browser audit events with safe export, redaction, and integrity metadata.
- OCR evidence integration produces no-authority evidence and writes OCR-related audit events without granting action permissions.

Agent Operations currently preserves evidence references through `NexaEvidenceRef`, but those references do not yet carry source, use, authority, sensitivity, redaction state, ledger identity, or provenance.

## OCR Evidence Path

OCR evidence already follows the right authority boundary:

- OCR evidence is observational.
- OCR evidence can support verification.
- OCR evidence cannot authorize actions.
- Raw image persistence and sensitivity are governed by OCR evidence policy.
- Audit events are written through `BrowserPersistentAuditLedger` as evidence/audit data, not as permission decisions.

The Agent Operations bridge should mirror this no-authority pattern instead of inventing a parallel authority model.

## BrowserPersistentAuditLedger Path

`BrowserPersistentAuditLedger` is the browser audit trail path. It stores redacted audit events with policy and integrity metadata. It is not a replacement for Agent Operations references, and Agent Operations should not write directly to it as a side effect in this milestone.

M383-M385 therefore uses an adapter-style bridge contract with optional `LedgerRef`.

## Gaps In NexaEvidenceRef

`NexaEvidenceRef` is intentionally lightweight and compatible, but it lacks:

- source kind
- use kind
- no-authority classification
- acceptance state
- sensitivity
- redaction state
- ledger reference identity
- provenance
- diagnostic versus auxiliary versus verification support classification

These gaps are acceptable for in-memory contracts, but they become unsafe before UI/orchestration/persistence unless bridged explicitly.

## Duplication Risk

Replacing the ledger would be wrong. The correct design is:

- keep `NexaEvidenceRef` for compatibility,
- add bridge contracts for richer metadata,
- preserve optional ledger identity in this design phase,
- avoid persistence side effects,
- avoid action authorization semantics.

## Decision

Create `NodalOsEvidenceRefBridge` as an adapter, not a replacement.

The bridge maps lightweight refs into `NodalOsEvidenceBridgeRef` values with:

- source kind,
- evidence use kind,
- no-authority model,
- sensitivity,
- redaction state,
- optional ledger reference,
- provenance.

## APIs Kept

Existing Agent Operations contracts keep using `NexaEvidenceRef`. No broad rename, namespace move, or ledger rewrite is performed.

## Not Touched

- No runtime behavior.
- No UI.
- No orchestration API.
- No recipe execution.
- No new persistence DB.
- No EvidenceLedger rewrite.
- No namespace/project move.
- No legacy delete.
