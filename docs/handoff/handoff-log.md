# NODAL OS Handoff Log

Date: 2026-07-07

Purpose: rolling handoff index. Future blocks should add one entry here by default instead of creating one handoff file per micro-block.

## Current Handoff Canon

- Current architecture summary: `docs/architecture/nodal-os-current-local-internal-architecture.md`.
- Documentation governance: `docs/architecture/nodal-os-documentation-governance.md`.
- Simplification backlog: `docs/architecture/nodal-os-simplification-backlog.md`.
- `/run` claim reconciliation: `docs/audit/nodal-os-run-claim-coherence-reconciliation.md`.

## Recent Canonical Handoffs To Keep Visible

| Area | Handoff |
| --- | --- |
| Active durable read precedence decision matrix | `docs/handoff/nodal-os-active-durable-read-precedence-latest-pointer-product-exposure-decision-matrix-design-only-handoff.md` |
| Durable latest-state auxiliary evidence audit | `docs/handoff/nodal-os-durable-latest-state-auxiliary-evidence-not-precedence-not-authority-external-audit-read-only-handoff.md` |
| Durable latest-state auxiliary evidence implementation | `docs/handoff/nodal-os-durable-latest-state-auxiliary-evidence-not-precedence-not-authority-implementation-handoff.md` |
| Reader candidate audit | `docs/handoff/nodal-os-durable-latest-state-reader-candidate-not-authority-external-audit-read-only-handoff.md` |
| Manifest create-only audit | `docs/handoff/nodal-os-durable-latest-state-manifest-create-only-external-audit-read-only-handoff.md` |
| Latest-state snapshot implementation | `docs/handoff/nodal-os-local-operator-surface-latest-state-snapshot-implementation-handoff.md` |
| Global claim reconciliation and writer concurrency | `docs/handoff/nodal-os-global-safety-claim-reconciliation-and-product-ledger-writer-concurrency-hardening-handoff.md` |

## Archive/Legacy Rule

Older handoffs remain traceability. Mark as archive/legacy if they:

- repeat a QA report exactly;
- describe a design-only block later superseded by implementation;
- only restate anti-capabilities already captured in canonical architecture;
- contain historical repo-wide runtime claims superseded by `/run` claim reconciliation.

## Future Handoff Rule

Default:

- one handoff log entry per block;
- create a new handoff file only for a major capability, external audit packet, or release gate;
- always include scope for read-only/no-runtime/no-execution claims;
- never use `NO_RUNTIME_NO_EXECUTION` as a repo-wide current claim while Pilot `/run`, ChromeLab/CDP or other lab/dev runtime footprints exist.
