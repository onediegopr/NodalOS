# NODAL OS Private Preview Run M124-M126

## Status

Internal local private preview run executed within `ReadyWithRestrictions`.

## Commit

`c7cc1d46437f182cd19793af4e788d75ebb97687`

## Scope

Allowed scope:

- Product/Admin local
- private local API in-process
- readiness dashboard
- diagnostics/evidence review
- issue triage local
- operator blocker explanations
- controlled internal local tests

## Allowed Flows Executed

- Product/Admin local readiness dashboard reviewed
- evidence/log summary reviewed
- M51/M65 scoped status reviewed
- active blockers reviewed
- operator blocker explanations reviewed
- issue triage local reviewed
- diagnostics reviewed
- private local API in-process status reviewed

## Blocked Flows Observed

- public SaaS
- public API
- real billing/email
- real credentials
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external CDP general-ready

## Evidence Refs

- `release-gate:ReadyWithRestrictions`
- `skipped-category-audit:passed`
- `m51:http-readonly-ledger:verified`
- `m65:target-owned-cdp-ledger:verified`
- `operator-ux:ready`

## Issues Found

- `pp-ux-001`: low UX issue. Operator copy can be clearer around target-owned proof versus external general-ready.

## Decision

Continue internal preview with minor fixes.

## What Was Not Tested

- production
- SaaS public
- public API
- real billing/email
- real credentials
- real customer data
- sensitive sites
- submit/pay/sign/delete
- recorder/replay productive
- new external targets
- external CDP general-ready
- embedded runtime
- Chromium fork
