# Product Ledger Real Minimal Redaction Retention Behavioral Gates

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_REAL_MINIMAL_REDACTION_RETENTION_BEHAVIORAL_GATES_READY`

## Context

The external audit carry-forward `MA-03_REAL_MINIMAL_REDACTION_RETENTION_BEHAVIORAL_GATES` found that Product Ledger activation flags could be read as caller-attested booleans rather than behavioral controls. This block adds a local deterministic guard before Product Ledger metadata persistence.

## Decision

Add `ProductLedgerPathLocalOnlyMetadataGuard` and integrate it into `ProductLedgerPathLocalOnlyActiveWriter`.

The guard:

- redacts secret-like metadata values before append;
- redacts bearer-like tokens, email-like values, connection-string-like values and long high-entropy values;
- normalizes sensitive keys into safe `redaction.fieldNN` entries;
- adds safe `redaction.applied`, `retention.mode`, `retention.max-fields` and `retention.max-value-chars` markers;
- blocks raw payload/content keys, local paths, oversized metadata, too many fields, unbounded retention claims and compliance/custody overclaims;
- enforces local ledger entry and byte ceilings before append.

## Boundary

This is minimal local-only behavioral redaction/retention guarding. It is not KMS, not WORM, not external trust, not compliance-grade custody, not legal deletion lifecycle, not cloud retention and not release/commercial readiness.

Payload remains hash-only. Metadata is bounded and redacted before persistence.

## Caller Attestation

Still caller-attested:

- authority evidence;
- failure/replay/rollback evidence;
- activation evidence references.

No longer caller-attested only:

- persisted Product Ledger redaction metadata safety;
- persisted Product Ledger retention/bounded metadata policy.

## Readiness

| Area | Updated status |
| --- | --- |
| Product Ledger local-only core | `92-95%` |
| Evidence/Timeline/Audit Trail | `local-only behavioral redaction/retention guard integrated` |
| Local-only internal product | `50-59%` |
| Usable end-to-end local product | `20-32%` |
| UI/operator surface | `15-25%` |
| External/cloud | `0%` |
| Release/commercial | `0%` |

## Findings

P0: 0

P1: 0

P2: 0 after MA-03 fix.

P3:

- Retention remains bounded-entry policy, not deletion lifecycle.
- Broader property/corpus integration can still harden cross-surface evidence.

P4:

- Local filesystem evidence remains same-boundary local evidence, not WORM/KMS custody.
- Redaction is deterministic minimal guard, not comprehensive DLP/compliance scanning.
