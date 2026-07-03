# Durable External Checkpoint Trust Design-Only

Date: 2026-07-03

Decision: `GO_WITH_FINDINGS_DURABLE_EXTERNAL_CHECKPOINT_TRUST_DESIGN_ONLY_READY`

## Scope

Design-only trust model for a future external checkpoint boundary for Durable Audit Trail.

This document does not implement checkpoint writing, checkpoint verification, WORM, KMS, cloud storage, DB persistence, network calls, provider integration, runtime/product enablement, service registration, command handlers, UI actions or release/commercial readiness.

## Problem

Local hash-chain verification can prove internal consistency of the ledger presented to it. Local-temp caller-held checkpoints can show that a current head regressed relative to a previous caller-held head. Neither provides independent external trust if the ledger and checkpoint can be replaced together inside the same local trust boundary.

## Future Trust Levels

| Level | Name | Evidence | Claim allowed |
| --- | --- | --- | --- |
| T0 | No checkpoint | Current hash-chain verification only | Internal consistency only. |
| T1 | Caller-held local-temp checkpoint | Previous local checkpoint compared to current head | Local regression suspicion only. |
| T2 | Local signed checkpoint file | Future local signature/key design plus monotonic record | Local tamper resistance, not WORM/compliance. |
| T3 | External append-only checkpoint sink | Future independent append-only service or media | External head continuity evidence. |
| T4 | WORM/KMS/compliance boundary | Future audited provider, key custody, retention and legal controls | Compliance-grade claim only after external audit and manual GO. |

Current implementation reaches only T1 in test-only/local-temp scope.

## Future Gates

Before any external checkpoint implementation:

- Trust boundary must be selected and documented.
- Key custody, rotation, revocation and recovery must be specified.
- Failure/rollback/non-rollback behavior must be specified.
- Redaction-before-checkpoint material must be proven.
- Product ledger path and runtime/product wiring must remain blocked until manual GO.
- Negative tests must prove no DB/cloud/network/provider calls occur in test-only builds.
- External audit must pass with P0/P1 = 0.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product/live authority or scope leak. |
| P1 | 0 | No productive checkpoint sink, provider, DB, network, registration, handler, UI action or release/commercial claim. |
| P2 | 0 | Design clarifies trust levels and implementation gates. |
| P3 | 3 | T2-T4 remain design-only. Key custody remains unassigned. Future provider/cloud choices require product/security decisions. |
| P4 | 1 | Percentages remain planning estimates. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Local-temp checkpoint evidence | 88-91% |
| External checkpoint trust design | 45-55% |
| External checkpoint implementation | 0% / NO-GO |
| WORM/KMS/cloud trust | 0% / NO-GO |
| Runtime/live product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Next Safe Option

`NODAL_OS_DURABLE_EXTERNAL_CHECKPOINT_TRUST_DESIGN_EXTERNAL_AUDIT_READ_ONLY`
