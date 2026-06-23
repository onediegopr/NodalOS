# M701 Evidence Completion Plan + Manual Screenshot Contract

Milestone: M701

Decision: `EVIDENCE_COMPLETION_PLAN_READY`

## Missing Evidence

Human evidence remains partial. Required before package freeze:

- Chrome extensions page with `NODAL OS Public Candidate` visible.
- Sidepanel showing token-present state and WebSocket connected or exact state.
- Runtime tab evidence.
- Service Worker DevTools evidence.
- CSP/console cleanliness summary.
- Permission warning if Chrome shows it.
- Bridge liveness status for localhost or 127.0.0.1 only.

## Screenshot Contract

Required screenshots or equivalent redacted evidence:

- extension loaded in Chrome;
- sidepanel connected/local bridge;
- runtime tab;
- service worker DevTools;
- permissions if shown.

## Redaction

Do not include API keys, authorization tokens, browser session data, raw long logs, personal data, or full ExtensionToken value.

If secret material appears, package freeze must remain blocked.
