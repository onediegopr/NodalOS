# Product Ledger Integration Property Test Pack

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_INTEGRATION_PROPERTY_TEST_PACK_READY`

## Context

The Product Ledger local-only writer already had local path activation, bounded append, checkpoint, read/verify, concurrency and minimal redaction/retention guards. This block adds a larger integration/property/adversarial test pack before putting any broader local operator route on top.

## Decision

Add `ProductLedgerIntegrationPropertyTestPackTests` as a Safety test pack covering:

- activation -> append -> checkpoint -> read/verify -> bounded local report export;
- operator acceptance matrix and local visual evidence linkage;
- deterministic metadata corpus handling for unicode values, markdown-like values, JSON-like values, secret-like values, connection-string-like values and high-entropy values;
- fail-closed rejection for empty keys, duplicate logical keys, Windows paths, UNC paths, URL-like values, raw payload/content values, control characters, oversized metadata and retention/external-trust overclaims;
- tamper detection for entry body, checkpoint, tail deletion, duplicate sequence, previous-hash mismatch, metadata tamper, redacted value tamper, partial line and malformed JSONL line;
- static write-surface allowlist for Core/Approval files.

## Hardening

The metadata guard now blocks raw payload/content values, URL/provider/file-like values, control characters and case-insensitive duplicate logical keys.

`ProductLedgerPathLocalOnlyActiveWriter.ReadVerified` now validates persisted metadata as already safe instead of accepting metadata merely because it could be redacted by the guard. Rehashed ledgers with sensitive persisted keys/values fail closed.

## Boundary

No productization claim is made. The pack remains local-only/test-only and does not add public UI, public route, cloud/provider/network, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes automation, destructive action, unbounded export/write or release/commercial readiness.

Bounded local report export remains a local diagnostic artifact with post-write hash verification, not compliance-grade custody.

## Readiness

| Area | Updated status |
| --- | --- |
| Product Ledger local-only core | `94-96%` |
| Approval/Human Review | `line-scoped local-only evidence unchanged` |
| Evidence/Timeline/Audit Trail | `local append/checkpoint/export/evidence linkage tested` |
| Runtime/Command/Execution | `unchanged, Pilot default-blocked explicit opt-in` |
| UI/operator surface | `15-25%` |
| Local-only internal product | `51-60%` |
| Usable end-to-end local product | `22-34%` |
| External/cloud | `0%` |
| Release/commercial | `0%` |

## Findings

P0: 0

P1: 0

P2: 0 after hardening.

P3:

- Future local operator route can reuse this pack but still needs route-specific negative tests.
- Deletion lifecycle remains not implemented.
- Broader property fuzzing can expand beyond the deterministic corpus.

P4:

- Static scans include historical/string false positives and require classification.
- Local filesystem evidence remains same-boundary local evidence, not WORM/KMS/compliance custody.
