# NODAL OS QA Log

Date: 2026-07-07

Purpose: rolling QA index. Future blocks should prefer one entry here over creating a new QA directory unless a block produces substantial machine-readable evidence.

## Current Canonical State

- Product Ledger local-only line is bounded/local/internal unless explicitly stated otherwise.
- Product Ledger approval/snapshot/manifest/reader/auxiliary evidence path does not execute commands.
- Pilot `/run` is separate: it is blocked by default and is an allowlisted local execution path when explicitly enabled.
- Release/commercial readiness remains `0% / NO-GO`.

## Latest Canonical QA Evidence

| Area | Latest evidence | Status |
| --- | --- | --- |
| Full-system bloat/product readiness audit | `docs/audit/nodal-os-full-system-cloud-editorial-bloat-architecture-audit/report.md` | GO_WITH_FINDINGS |
| Documentation compaction Block A | this log plus architecture docs | GO_WITH_FINDINGS |
| `/run` claim reconciliation | `docs/audit/nodal-os-run-claim-coherence-reconciliation.md` | GO_WITH_FINDINGS |
| Product Ledger latest-state auxiliary evidence | `docs/qa/product-ledger-durable-latest-state-auxiliary-evidence-not-precedence-not-authority-implementation/report.md` | GO_WITH_FINDINGS |
| Durable latest-state decision matrix | `docs/qa/product-ledger-active-durable-read-precedence-latest-pointer-product-exposure-decision-matrix-design-only/report.md` | GO_WITH_FINDINGS |
| Global safety claim reconciliation | `docs/qa/nodal-os-global-safety-claim-reconciliation-and-product-ledger-writer-concurrency-hardening/report.md` | GO_WITH_FINDINGS |
| Source refactor readiness audit | `docs/architecture/nodal-os-source-refactor-readiness-audit.md` | GO_WITH_FINDINGS |

## Archive/Legacy Rule

Existing QA directories remain traceability. They are not deleted in Block A. A future archive block may compact older QA directories by area:

- Product Ledger path/writer.
- Approval and handoff writers.
- Latest-state evidence.
- Runtime/Pilot/ChromeLab claim reconciliation.
- Durable Stage 2 and audit trail.
- Historical pause/closeout packets.

## Future QA Rule

Default:

- one QA log entry per block;
- no new QA directory unless the block needs JSON, screenshots, generated artifacts or multi-file evidence;
- no duplicate handoff text in QA;
- explicitly state scope when using phrases like read-only, no-runtime, no-execution or no-command-execution.
