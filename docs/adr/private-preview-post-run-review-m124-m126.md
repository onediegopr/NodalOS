# ADR: Private Preview Post-Run Review M124-M126

## Status

Accepted: continue internal preview with minor fixes.

## What Was Tested

The first NODAL OS internal local private preview run exercised:

- Product/Admin local readiness dashboard review
- private local API in-process status review
- diagnostics and evidence/log summary review
- M51 and M65 scoped status review
- active blocker visibility
- operator blocker explanations
- local issue triage
- release gate `ReadyWithRestrictions`

## What Was Not Tested

This run did not test or enable:

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
- HITO-162 rewrite

## Issues Found

One low UX issue was captured:

- `pp-ux-001`: operator-facing copy should more clearly distinguish target-owned proof from external general-ready.

No critical/high security issue was found.

No scope inflation was found.

No evidence/log blocker was found.

## Decision

`ContinueWithMinorFixes`.

Internal local private preview can continue under `ReadyWithRestrictions`.

## Required Follow-Up

- improve operator copy in a later UX hardening block;
- keep all active blockers visible;
- keep M51/M65 scoped and non-general;
- keep external CDP general-ready false;
- do not open production/SaaS/public API/billing/email/credentials/sensitive surfaces.

## Active Blocks

- public SaaS
- public API
- real billing/email
- real credentials
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external CDP general-ready
- new external targets without dedicated evidence
- embedded runtime
- Chromium fork
- HITO-162
