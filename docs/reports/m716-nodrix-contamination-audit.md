# M716 NODRIX Contamination Audit

## Decision

M716 completed the NODRIX contamination audit for the active NODAL OS release line.

## Scope

Active project: NODAL OS.

NODRIX is frozen, out of scope, and must not be used as an active product, roadmap, store, package, or release source of truth.

## Findings

- Store listing drafts are clean and use NODAL OS as active product.
- Package freeze docs had two active-line exclusion mentions using the NODRIX name.
- Those two mentions were classified as `contamination_to_fix` because they unnecessarily named an out-of-scope project in active package documentation.
- Historical NODRIX references in prior naming audits and guardrail tests are allowed as report context.
- HOTEP references are external uploaded input context.
- NEXA and ONE BRAIN references are historical, compatibility, legacy filename, or guard-test context.

## Patch

Corrected:

- `artifacts/agent-operations/m652/package-contents-manifest.json`
- `docs/reports/m652-packaging-candidate-artifact-prep.md`

Replacement: `out-of-scope external project files`.
