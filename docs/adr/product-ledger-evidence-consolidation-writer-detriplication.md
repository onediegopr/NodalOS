# Product Ledger Evidence Consolidation and Writer De-Triplication

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LEDGER_EVIDENCE_CONSOLIDATION_WRITER_DETRIPLICATION_READY`

## Context

The local-only Product Ledger line had three writer-shaped components and adjacent Durable Audit Trail concepts:

- `ProductLedgerPathWriterScaffoldDisabled`
- `ProductLedgerPathLocalTempWriterTestOnly`
- `ProductLedgerPathLocalOnlyActiveWriter`
- `DurableAuditTrailAppendOnlyMinimal`
- `DurableAuditTrailLocalTempCheckpointEvidence`

The command router/handler also used "handler" wording while most commands are preview/no-op/read-only and one bounded local report export delegates to the isolated export service.

## Decision

Keep `ProductLedgerPathLocalOnlyActiveWriter` as the single active local-only Product Ledger authority.

Introduce:

- `ProductLedgerLocalAppendOnlyHashing` as the shared hash-chain/ledger-hash helper used by the active writer and local-temp test-only writer.
- `ProductLedgerLocalLedgerTaxonomy` as code-level taxonomy for ledger modes, roles and authority boundaries.
- `ProductLedgerEvidenceConsolidationTests` as regression coverage for taxonomy, hashing compatibility, scaffold non-authority, Durable non-authority and command preview guarantees.

Do not rename or delete public classes in this block. The compatibility risk is higher than the value while historical QA/ADR/handoff files and tests still reference those names.

## Canonical Taxonomy

| Mode | Component | Status |
| --- | --- | --- |
| `LocalOnlyActive` | `ProductLedgerPathLocalOnlyActiveWriter` | Single active local-only Product Ledger authority. |
| `LocalTempTestOnly` | `ProductLedgerPathLocalTempWriterTestOnly` | Test-only/local-temp writer, not a product ledger path. |
| `DisabledScaffoldHistorical` | `ProductLedgerPathWriterScaffoldDisabled` | Historical disabled/test-only scaffold, non-authoritative. |
| `DurableAuditTrailTestOnly` | `DurableAuditTrailAppendOnlyMinimal` | Durable test-only sibling, not competing Product Ledger authority. |
| `FutureProductiveLocalOnly` | future placeholder | Not active; requires separate GO. |

## Command Preview Boundary

`ProductLedgerInternalCommandHandler` remains source-compatible, but its status and result model now expose preview-only/public-execution-false/product-command-execution-false guarantees.

Most commands remain no-op/read-only/in-memory. The bounded physical export command is still isolated through `ProductLedgerLocalReportExportService`, remains local-only/internal-only/bounded, and does not create public command exposure.

## Boundary

No ledger format changed. No historical evidence files were deleted. No new runtime capability, productization, public UI action, public route, provider/cloud/network, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes automation, destructive action, unbounded export/write, external/cloud export or release/commercial readiness was introduced.

## Readiness

| Area | Updated status |
| --- | --- |
| Product Ledger local-only core | `94-96%` |
| Approval/Human Review | `line-scoped local-only evidence unchanged` |
| Evidence/Timeline/Audit Trail | `improved taxonomy coherence; Product Ledger remains canonical active local ledger` |
| Runtime/Command/Execution | `preview-only/no public product command execution; Pilot default-blocked explicit opt-in` |
| UI/operator surface | `15-25%` |
| Local-only internal product | `52-61%` |
| Usable end-to-end local product | `22-34%` |
| External/cloud | `0%` |
| Release/commercial | `0%` |

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Public names remain for compatibility; future cleanup can rename with a migration pack.
- Local-temp writer still has its own entry/checkpoint DTOs.
- Deletion lifecycle remains outside this block.

P4:

- Durable Audit Trail and Product Ledger still have sibling concepts; taxonomy prevents authority confusion but does not merge subsystems.
- Historical docs continue to mention old names for traceability.
