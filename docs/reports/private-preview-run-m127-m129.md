# NODAL OS Private Preview Run M127-M129

## Status

Second internal local private preview run executed within `ReadyWithRestrictions`.

## Commit

`8125c37d29d0456dca32bcbc99939ed4c6f9a051`

## M127 Fix Verification

`pp-ux-001` is fixed and verified.

The operator-facing copy now states:

- M65 is closed only for `lab.nodalos.com.ar` target-owned Chrome/CDP/DOM read-only proof.
- M65 does not authorize external CDP general-ready.
- Third-party, sensitive, credential, production, and irreversible browsing remain blocked.

## Allowed Flows Executed

- Product/Admin local readiness dashboard reviewed
- evidence/log summary reviewed
- M51/M65 scoped status reviewed
- active blockers reviewed
- operator blocker explanations reviewed
- issue triage local reviewed
- diagnostics reviewed
- private local API in-process status reviewed

## New Issues

No new issues were captured in this run.

## Blocked Flows Observed

- public SaaS
- public API
- real billing/email
- real credentials
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external CDP general-ready
- new external targets without dedicated evidence

## Evidence Refs

- `release-gate:ReadyWithRestrictions`
- `skipped-category-audit:passed`
- `m51:http-readonly-ledger:verified`
- `m65:target-owned-cdp-ledger:verified`
- `operator-ux:pp-ux-001-fixed`

## Decision

Continue internal preview stable.

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
- HITO-162 rewrite
