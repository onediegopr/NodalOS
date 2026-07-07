# NODAL OS Documentation Governance

Date: 2026-07-07

## Purpose

Stop documentation bloat from becoming a second product. Future documentation must clarify current authority, not multiply closeout artifacts.

## Rules

1. Create an ADR only for a real architectural decision.
2. Prefer `docs/qa/qa-log.md` over a new QA directory.
3. Prefer `docs/handoff/handoff-log.md` over a new handoff file.
4. Use audit read-only docs only when a block changes risk posture or creates a reusable audit packet.
5. Design-only docs must either expire, be promoted, or be archived/indexed.
6. Every new boundary must justify why it is not an enum/status field in an existing model.
7. Every read-only/no-runtime/no-execution claim must state its scope.
8. Do not use repo-wide `ZeroReadOnly` or unscoped `NO_RUNTIME_NO_EXECUTION` while Pilot `/run` and lab/dev runtime footprints exist.
9. Do not create ADR+QA+handoff triplets for repetitive no-change audits.
10. Historical docs remain traceability; canonical docs describe current truth.

## Maximum Recommended Artifacts Per Capability

- 1 ADR or addendum.
- 1 QA log entry.
- 1 handoff log entry.
- Focused tests.
- Optional JSON only when machine-readable evidence is needed.
- No triple redundant docs by default.

## Required Canonical Links

Each major block should link to:

- current architecture summary;
- ADR canonical index;
- QA log;
- handoff log;
- decision-log entry if risk posture changed.

## Claim Wording

Allowed:

- "Product Ledger operator surface does not execute commands."
- "`/run` is a gated allowlisted local execution path."
- "This surface is read-only."
- "This Product Ledger boundary has no command execution."

Forbidden in current canonical docs:

- "Repo-wide ZeroReadOnly."
- "`NO_RUNTIME_NO_EXECUTION`" without scope.
- "No runtime execution anywhere."
- "`/run` is read-only."

## Archive Policy

Archive moves require a separate docs-only block. Do not delete history until:

1. the item appears in an index;
2. links are preserved or replaced;
3. security load-bearing content is kept visible;
4. `git diff --check` and any link/static checks pass.
