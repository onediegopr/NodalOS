# ADR: Private Preview Stabilization Review M127-M129

## Status

Accepted: continue internal preview stable.

## Previous Issue Status

`pp-ux-001` is fixed and verified in the second internal local private preview run.

The fix was limited to operator-facing copy:

- M65 is target-owned only.
- M65 does not authorize external CDP general-ready.
- External third-party, sensitive, credential, production, and irreversible flows remain blocked.

No gate semantics were relaxed.

No scope expansion was introduced.

## Second Run

The second internal local private preview run exercised:

- Product/Admin local readiness dashboard review
- evidence/log summary review
- M51/M65 scoped status review
- active blocker review
- operator blocker explanations
- local issue triage
- diagnostics review
- private local API in-process status review

## New Issues

No new issues were captured.

## Decision

`ContinueInternalPreviewStable`.

NODAL OS can continue internal local private preview under `ReadyWithRestrictions`.

## Active Blocks

- production / SaaS public
- public API
- real billing/email
- real credentials
- real customer data
- sensitive sites
- submit/pay/sign/delete
- productive recorder/replay
- external CDP general-ready
- new external targets without dedicated evidence
- embedded runtime
- Chromium fork
- HITO-162

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
- external CDP general-ready
- embedded runtime
- Chromium fork
- HITO-162 rewrite

## Recommended Next Blocks

- M130+M131+M132: Product/Admin usability polish and internal preview iteration 2.
- M133+M134+M135: HITO-162 rewrite/map.
- M136+M137+M138: broader private preview readiness, still local only.

Embedded runtime/WebView2/CEF remains future work, not now.
