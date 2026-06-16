# ADR - HITO-162 Replacement Final Review - M145-M147

## Status

Accepted.

## Decision

M133-M144 are accepted as the local fixture-first replacement for legacy HITO-162.

Decision: Hito162ReplacementStable.

## Scope

The replacement is stable for local private preview signals:

- identity/fingerprint
- robust perception
- safe action boundary
- process memory/workflow learning

It is not production readiness and does not open external general CDP.

## Next Phase

Recommended next phase: Product/Admin polish and second internal private preview iteration.

Acceptable next options:

- ContinueLocalPreviewIteration
- ProductAdminPolish
- Hito162ReplacementStable
- PrepareProcessMemoryIteration2

Not now:

- embedded runtime evaluation
- Chromium fork
- external general CDP
- SaaS/public API/billing/email/credentials/sensitive-site expansion

## Rationale

The replacement sequence now has contracts, fixtures, evidence summaries, operator-facing signals, ADRs, roadmap entries, and regression tests. The next bottleneck is product/operator usability and internal preview iteration, not more low-level replacement mapping.

## Guardrails

Core remains authoritative. UI/Admin/Companion are non-authoritative. Identity, perception, safe action evidence, and process memory remain signals/gates only.

