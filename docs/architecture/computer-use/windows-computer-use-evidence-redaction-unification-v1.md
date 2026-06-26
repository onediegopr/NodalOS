# Windows Computer Use Evidence/Redaction Unification v1

Status: fixture-safe design and implementation.

## Purpose

The unified evidence pack collects read-only metadata from WCU fixture signals and normalizes redaction before export/reporting. Evidence cannot unlock action authority.

## Evidence Sources

`ComputerUseUnifiedEvidencePackBuilder` can reference:

- UIA fixture snapshot;
- UIA element identity;
- Win32 context;
- UIA event stream;
- OCR/visual observations from the existing bridge;
- locator fusion result;
- sensitive/blockage result;
- safe action dry-run plan;
- perception fusion result;
- future verification expectation fixture metadata.

## Redaction Order

1. Raw fixture input.
2. Redaction via `ComputerUseEvidenceRedactor`.
3. Evidence metadata construction.
4. Report/export serialization.

Locator fusion also redacts candidate identity fields before exposing them in serializable results.

## Prohibited Evidence

Evidence packs must not contain:

- raw screenshot bytes;
- clipboard content;
- unredacted secrets;
- unredacted window titles with user/customer data;
- unredacted process paths with usernames;
- credential field values;
- raw OCR text;
- live action traces.

## Metadata

Unified evidence includes:

- `SourceSignals`;
- `RedactionStatus`;
- `RawScreenshotPresent=false`;
- `ClipboardPresent=false`;
- `ActionAuthorityGranted=false`;
- `RequiresHumanHandoff`;
- `ConfidenceSummary`;
- `TamperGuardHash`;
- `AuditLogBypassGuard=true`.

## Tamper and Bypass Guard

The pack computes a SHA-256 tamper guard over snapshot id, redacted summary, evidence refs, and confidence summary. This is fixture metadata integrity only; it is not an execution approval.

Audit log bypass guard remains enabled when hostile source flags are observed. Hostile flags create blockages and handoff instead of execution.

## Authority Boundary

No evidence ref, confidence summary, tamper hash, redaction status, or dry-run plan can authorize actions. Future action authority requires separate policy, approval, and gated executor work outside this block.
