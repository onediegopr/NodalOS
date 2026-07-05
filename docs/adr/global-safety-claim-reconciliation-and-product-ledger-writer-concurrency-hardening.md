# Global Safety Claim Reconciliation And Product Ledger Writer Concurrency Hardening

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_GLOBAL_SAFETY_CLAIM_RECONCILIATION_AND_PRODUCT_LEDGER_WRITER_CONCURRENCY_HARDENING_READY`

## Context

An external mega audit found that Product Ledger local-only evidence was coherent, but the repo-level safety narrative could be read too broadly. Pilot `/run` can execute allowlisted recipes through a process runner when explicitly invoked, and ChromeLab/CDP code exists as a separate lab/dev runtime footprint. Therefore repo-wide inert/read-only wording is not honest.

The same audit found that the Product Ledger local-only active writer computed sequence and previous-hash values without a per-path critical section. Concurrent appends could race and create duplicate sequence or invalid checkpoint evidence.

## Decision

- Treat Product Ledger readiness percentages as Product Ledger local-only line scoped.
- Treat Pilot, ChromeLab and CDP as separate lab/dev runtime footprints outside Product Ledger local-only authority.
- Block Pilot `/run` by default unless `NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION=1` is explicitly set.
- Relabel Pilot safety summary as lab/dev runtime footprint default-blocked rather than inert/read-only.
- Serialize Product Ledger active writer appends by canonical ledger file path.
- Keep MA-03 as a carry-forward: caller-attested redaction/retention evidence is not behavioral enforcement.

## Boundary

Not enabled:

- public deploy or public internet exposure;
- provider/cloud/network;
- telemetry/sync/billing cloud;
- DB/migration;
- KMS/WORM/external trust;
- productive Browser/CDP/WCU/OCR/Recipes live automation;
- destructive user-facing action;
- unbounded export/write;
- release/commercial readiness.

## Readiness Scoping

| Area | Updated status |
| --- | --- |
| Product Ledger local-only core | `88-92%` |
| Local-only internal product | `48-57%` |
| Usable end-to-end local product | `20-30%` |
| UI/operator surface | `15-25%` |
| External/cloud | `0%` |
| Release/commercial | `0%` |

## Findings

P0: 0

P1: 0 after MA-01 fix.

P2: 0 after MA-02 fix.

P3:

- MA-03 real minimal redaction and retention behavioral gates remain future work.
- Pilot, ChromeLab and CDP remain lab/dev runtime footprints and require continued claim isolation.

P4:

- Existing older docs may contain historical pause labels and must be read as traceability, not current repo-wide claims.
- Product Ledger local filesystem evidence is not WORM/KMS/compliance-grade custody.
