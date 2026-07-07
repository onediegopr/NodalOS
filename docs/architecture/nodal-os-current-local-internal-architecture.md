# NODAL OS Current Local/Internal Architecture

Date: 2026-07-07

Status: canonical current-state summary for the local/internal line.

## Current Real State

NODAL OS currently contains a local-only Product Ledger line with persisted approval state, bounded local evidence writers, a dev-gated operator surface, and durable latest-state evidence artifacts. It also contains separate Pilot, ChromeLab, Browser/CDP, WCU/OCR and legacy/runtime-shaped footprints that must not be collapsed into a repo-wide read-only claim.

Release/commercial readiness remains `0% / NO-GO`.

## Implemented

- Product Ledger local-only path and append/verify/checkpoint kernel.
- Approval decision state.
- Approved no-op execution.
- Bounded internal marker.
- `LocalApprovedHandoffReportDraft`.
- `LocalWorkspaceTestJailHandoffDraftCreateOnly`.
- `LocalUserWorkspaceAllowlistedHandoffDraftCreateOnly` under `docs/nodal-os/handoffs/`.
- Latest-state snapshot create-only.
- Latest-state manifest create-only.
- Latest-state reader candidate not-authority.
- Local durable latest-state auxiliary evidence not-precedence/not-authority.
- Redaction-before-persistence service for persisted Product Ledger metadata.
- Dev-gated Product Ledger operator surface/read model.
- Pilot `/run` route exists as a separate gated allowlisted local execution surface, not a Product Ledger approval path.

## Design-Only

- Active durable read precedence candidate not-product-authority.
- Latest pointer behavior.
- Product read-model authority.
- Public/product exposure.
- Production route.
- Broader workspace action.
- Source contract/model consolidation.
- Route consolidation.
- Documentation archive/move plan.

## Test-Only / Guard-Only

- Static no-enable scans for Product Ledger command execution, public/product, Production route, provider/cloud/network, DB, KMS/WORM, release/commercial and live automation claims.
- Safety and Recipes route/DOM tests for local/dev Product Ledger surfaces.
- Design-only matrix guard tests.
- Historical WCU/OCR/browser runtime safety tests, separate from Product Ledger authority.

## Docs-Only

- Editorial bloat audit.
- Simplification plan.
- Documentation compaction map.
- ADR canonical index.
- QA/handoff logs.
- `/run` claim-coherence reconciliation.

## Blocked

- Active durable read precedence implementation.
- Latest pointer.
- Latest pointer overwrite.
- Product read-model authority.
- Durable authority.
- Live/product authority.
- Public/product exposure.
- Production route.
- Broader workspace action.
- Edit/update/delete.
- User-selected path outside authorized local handoff boundaries.
- New shell/subprocess or command execution.
- Browser/CDP/WCU/OCR/Recipes live product automation.
- Provider/cloud/network.
- DB/migration.
- KMS/WORM/external trust.
- Compliance custody.
- Release/commercial readiness.

## Local Chain

1. Product Ledger local-only.
2. Approval persisted.
3. Approved no-op execution.
4. Bounded internal marker.
5. Local approved handoff report draft.
6. Workspace test-jail handoff draft create-only.
7. User-workspace allowlisted handoff draft create-only.
8. Latest-state snapshot create-only.
9. Latest-state manifest create-only.
10. Reader candidate not-authority.
11. Auxiliary evidence not-precedence/not-authority.

## What Is Not Authority

- Snapshots are historical evidence only.
- Manifests are versioned create-only evidence indexes, not latest pointers.
- Reader candidates are not durable/product authority.
- Auxiliary evidence is not read precedence and not authority.
- Product Ledger approval path does not execute commands and is not Pilot `/run`.

## What Does Not Exist

- Active read precedence.
- Latest pointer.
- Latest pointer overwrite.
- Product authority.
- Public/product route.
- Production route.
- Broader workspace action.
- Release/commercial readiness.

## Essential Pieces Not To Prune

- Append-only hash-chain/checkpoint ledger semantics.
- Canonical path confinement.
- Fail-closed validation.
- Redaction-before-persistence.
- Approval decision persistence.
- Product Ledger local metadata guard.
- One dev-gated operator read model.
- `/run` claim isolation.
- One static no-enable guard intent.

## Future Merge/Simplification Candidates

- Snapshot/manifest/reader/auxiliary evidence into `LatestStateEvidence` with a `role` field.
- Writer variants into one writer plus `WriterMode`.
- Per-node `Request`/`Options`/`Result`/`Validation` into shared local-only result/claims/blocker models.
- Multiple operator surfaces into one `OperatorSurfaceReadModel`.
- Safety/Recipes mirror tests into required-smoke and extended suites.
- Repeated ADR/QA/handoff triplets into canonical index + rolling logs.
