# Durable External Checkpoint Trust Boundary Local-Only Test-Only

Date: 2026-07-03

Decision: `GO_WITH_FINDINGS_LOCAL_ONLY_CHECKPOINT_TRUST_BOUNDARY_TEST_ONLY_READY`

## Scope

This block closes the external checkpoint trust boundary decision as local-only, no-provider and test-only.

It allows local-temp checkpoint evidence validation for tests and design evidence only. It does not authorize cloud, KMS, external key custody, WORM media, provider/network integration, product checkpoint writing, product ledger paths, runtime/product enablement, service registration, command handlers, UI product actions, DB/migration, Browser/CDP/WCU/OCR/Recipes live execution, release readiness or commercial readiness.

## Boundary

The current implemented boundary remains `local-temp-test-only`.

Allowed:

- Local-temp ledger head capture from the existing minimal Durable Audit Trail verifier.
- Caller-held checkpoint comparison against the current local-temp ledger head.
- Local-only rejection of malformed or overclaimed checkpoint evidence.
- Test-only Safety and Recipes evidence.

Blocked:

- Cloud checkpoint sinks.
- KMS or external key custody.
- Real WORM or compliance-grade storage.
- Provider SDKs, network calls and DB-backed checkpoint stores.
- Product checkpoint writers, product ledger paths and runtime service registration.
- Release/commercial claims.

## Threat Model

Local-temp checkpoint evidence can detect a later local head regression when the caller still holds a previous checkpoint. It cannot prove independent external trust because ledger and checkpoint material can still be replaced together inside the same local trust boundary.

Therefore checkpoint evidence must fail closed if it claims any stronger boundary than local-temp/test-only. In particular, a caller-provided checkpoint that claims external trust, WORM/KMS backing, cloud backing or release/commercial readiness is rejected before head comparison.

## Implemented Test-Only Hardening

- `DurableAuditTrailLocalTempCheckpointEvidence.CompareHeadCheckpoint` validates caller-provided checkpoint evidence before comparison.
- Malformed checkpoint evidence is rejected.
- Checkpoints outside the local-temp boundary are rejected.
- Trust boundary mismatch is rejected.
- External trust, WORM/KMS/cloud and release/commercial claims are rejected.
- Source guards confirm no DI registration, command handlers, product actions, DB, network, provider SDK or product readiness wiring.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product/live authority or scope leak. |
| P1 | 0 | No provider/cloud/KMS/network/WORM/product checkpoint implementation was added. |
| P2 | 0 | Overclaimed checkpoint evidence now fails closed. |
| P3 | 2 | The trust boundary is still local-temp/caller-held only. External independent trust remains blocked by product/security policy. |
| P4 | 1 | Future T2-T4 taxonomy remains useful as blocked roadmap context only. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Local-only checkpoint trust boundary | 82-88% |
| Local-temp checkpoint evidence | 90-93% |
| External provider/cloud/KMS checkpointing | 0% / NO-GO |
| Product checkpoint/runtime enablement | 0% / NO-GO |
| WORM/compliance-grade trust | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Next Safe Option

`NODAL_OS_LOCAL_ONLY_CHECKPOINT_TRUST_BOUNDARY_EXTERNAL_AUDIT_READ_ONLY`
