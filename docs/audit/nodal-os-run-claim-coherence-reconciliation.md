# NODAL OS `/run` Claim-Coherence Reconciliation

Date: 2026-07-07

Decision: `GO_WITH_FINDINGS_RUN_CLAIM_COHERENCE_RECONCILED_DOCS_ONLY`

## Scope

Docs-only reconciliation. No code was changed. `/run` was not disabled or enabled by this document.

## What Exists

- `src/OneBrain.Pilot/Program.cs` maps `POST /run`.
- `POST /run` evaluates `PilotRecipeExecutionGate`.
- If the gate is disabled, the route renders a blocked/default result.
- If enabled, the route calls `PilotRecipeExecutor.ExecuteAsync`.
- Prior claim reconciliation documented that Pilot `/run` is blocked by default behind `NODAL_OS_ENABLE_PILOT_RECIPE_EXECUTION=1`.

## Where The Confusing Claims Appear

- Historical roadmap/closeout docs use labels like `PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION...`.
- Some historical handoffs use `CLOSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION`.
- The full-system bloat audit noted a carried claim-coherence issue around `ZeroReadOnly` and `NO_RUNTIME_NO_EXECUTION`.
- Product Ledger docs often say "no command execution" or "No Pilot `/run`"; those are scoped Product Ledger claims, not repo-wide claims.

## Reconciliation

Correct current wording:

- `/run` is NOT `ZeroReadOnly`.
- `/run` is a gated allowlisted local execution path when enabled under its own Pilot gates.
- Product Ledger approval/snapshot/local evidence path remains no-command-execution unless explicitly connected in a future authorized block.
- `NO_RUNTIME_NO_EXECUTION` applies only to specific surfaces/boundaries where verified, not repo-wide.
- Historical `PAUSED_READ_ONLY_NO_RUNTIME_NO_EXECUTION...` labels remain traceability and are superseded by this scoped claim canon.
- ChromeLab/CDP, Browser runtime, WCU/OCR and Pilot are separate lab/dev runtime footprints and must not be erased by Product Ledger local-only wording.

## False Or Incomplete Claims

| Claim | Status | Correct scope |
| --- | --- | --- |
| Repo-wide `ZeroReadOnly` | False/incomplete | Not valid while Pilot `/run` exists. |
| Repo-wide `NO_RUNTIME_NO_EXECUTION` | False/incomplete | Valid only for scoped surfaces that have no runtime/execution. |
| `/run` is read-only | False if execution gate is enabled | `/run` is a gated allowlisted local execution path. |
| Product Ledger approval path has no command execution | Correct | Product Ledger approval path scope only. |
| Durable latest-state evidence is no-command-execution | Correct | Snapshot/manifest/reader/auxiliary evidence scope only. |

## Allowed Wording

- "Product Ledger operator surface does not execute commands."
- "Durable latest-state snapshots/manifests are evidence only."
- "`/run` is a gated allowlisted local execution path."
- "No product command execution is enabled from Product Ledger approval path."
- "Pilot, ChromeLab/CDP and Browser/WCU/OCR runtime footprints are separate from Product Ledger local-only authority."

## Prohibited Wording In Current Canonical Docs

- "Repo-wide ZeroReadOnly."
- "`NO_RUNTIME_NO_EXECUTION`" without scope.
- "No runtime execution anywhere."
- "`/run` is read-only" when discussing the current Pilot route.

## Product Ledger Claim Boundary

The Product Ledger local-only line may continue to claim no command execution for:

- approval decision state;
- approved no-op execution;
- bounded internal marker;
- local handoff draft writers;
- latest-state snapshot/manifest/reader/auxiliary evidence;
- Product Ledger operator surface.

Those claims do not describe Pilot `/run`.

## Required Follow-Up Before Product Readiness

Before any product-readiness statement:

1. Keep this scoped wording in canonical architecture docs.
2. Avoid repo-wide "read-only/no-runtime/no-execution" banners.
3. If `/run` is exposed in product, run a separate GO window for runtime/product policy, UI wording, tests and release/commercial blockers.
