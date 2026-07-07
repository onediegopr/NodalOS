# NODAL OS ADR Canonical Index

Date: 2026-07-07

Purpose: reduce ADR noise by identifying the current canonical ADR set and archive candidates. This file does not delete or move any ADR.

## Canonical ADRs Recommended To Keep Visible

1. `global-safety-claim-reconciliation-and-product-ledger-writer-concurrency-hardening.md`
2. `product-ledger-path-local-only-active-writer.md`
3. `product-ledger-real-minimal-redaction-retention-behavioral-gates.md`
4. `redaction-before-persistence-service-test-only-external-audit-and-fixes.md`
5. `product-ledger-integration-property-test-pack.md`
6. `product-ledger-evidence-consolidation-writer-detriplication.md`
7. `product-ledger-single-real-local-operator-route-and-surface-consolidation.md`
8. `product-ledger-local-approval-real-operator-input-state-persistence.md`
9. `product-ledger-local-approved-no-op-execution-boundary.md`
10. `product-ledger-local-bounded-approved-action-boundary.md`
11. `product-ledger-local-approved-handoff-report-draft-implementation.md`
12. `product-ledger-workspace-test-jail-handoff-draft-implementation.md`
13. `product-ledger-user-workspace-allowlisted-handoff-draft-implementation.md`
14. `product-ledger-broader-user-workspace-action-or-public-product-exposure-boundary-design-only.md`
15. `product-ledger-local-operator-surface-latest-state-snapshot-implementation.md`
16. `product-ledger-durable-latest-state-manifest-create-only-implementation.md`
17. `product-ledger-durable-latest-state-reader-candidate-not-authority-implementation.md`
18. `product-ledger-durable-latest-state-auxiliary-evidence-not-precedence-not-authority-implementation.md`
19. `product-ledger-active-durable-read-precedence-latest-pointer-product-exposure-decision-matrix-design-only.md`
20. `browser-cdp-chromelab-runtime-boundary-design-only.md`
21. `durable-stage2-final-external-audit-roadmap-claim-reconciliation-read-only.md`
22. `durable-runtime-product-enable-premortem-decision-packet-read-only.md`
23. `durable-external-checkpoint-trust-boundary-local-only-test-only.md`
24. `implementation-planning-gate-design-only.md`
25. `phase-e-approval-human-review-formal-closeout.md`
26. `browser-authenticated-flow-sandbox-m24.md` as historical/sandbox context only.
27. `browser-safe-document-download-real-m26.md` as historical/export context only.
28. `durable-audit-trail-enablement-gate-design-only.md`

## Categories

### Product Ledger Core

Keep the active writer, redaction/retention, integration/property, evidence consolidation and route/surface consolidation ADRs. Archive older path scaffold, temp writer and disabled-writer precursors after an archive index exists.

### Approval

Keep approval decision state, no-op execution, bounded action and handoff writer ADRs. Archive duplicated external-audit read-only variants after their decisions are summarized in `qa-log.md` and `handoff-log.md`.

### Evidence

Keep latest-state snapshot, manifest, reader candidate and auxiliary evidence implementation ADRs. Archive repeated design-only/external-audit packets once the current architecture doc references them.

### Redaction

Keep `RedactionBeforePersistenceService` docs and behavioral Product Ledger redaction docs. Do not archive the only docs that prove redaction-before-persistence semantics.

### Path Confinement

Keep canonicalization/reparse/path-confinement docs until their rules are centralized in a single policy document and covered by tests.

### Operator Surface

Keep the single-route/surface consolidation ADR. Mark older preview/mock/public-read-only UI ADRs as archive candidates.

### Local Handoff Writers

Keep workspace test-jail and user-workspace allowlisted handoff implementation ADRs. Archive design-only precursors later.

### Snapshot / Manifest / Read Candidate

Keep implementation ADRs and the latest design matrix. Merge future docs into one `LatestStateEvidence` decision trail.

### Static Guards

Keep one canonical static no-enable guard record. Archive repeated per-boundary negative scan docs after a central scanner exists.

### Runtime `/run` / Pilot / ChromeLab

Keep claim reconciliation and runtime boundary ADRs. Current canon: Pilot `/run` is a separate gated allowlisted local execution path, not repo-wide read-only and not Product Ledger authority.

### Release / Commercial Blocked Claims

Keep release/commercial NO-GO docs until a real release gate exists. Do not convert local-only evidence readiness into release readiness.

## ADRs To Archive By Category

- Per-micro-block external audit read-only ADRs that repeat implementation docs.
- Design-only ADRs superseded by implementation and audit.
- Historical pause/closeout ADRs with broad runtime wording, after this index and the `/run` reconciliation are linked.
- Public/product exposure mock/disabled ADRs after product-surface simplification design is created.

## Duplicate Or Event-Only ADRs

Many ADRs record a macro-block event rather than an enduring architectural decision. Future policy: if a block only validates existing behavior, write a QA/handoff log entry instead of a new ADR.

## Security Load-Bearing ADRs

Do not drop docs that are the only source for:

- fail-closed rules;
- redaction-before-persistence;
- hash/checkpoint verification;
- local path confinement;
- Product Ledger no-command-execution boundary;
- Pilot `/run` claim scope;
- release/commercial NO-GO.

## Future Physical Archive Steps

1. Create an archive folder and index.
2. Move archive candidates in small batches.
3. Preserve links from this index.
4. Run link/static checks.
5. Do not mix archive moves with source refactor.
