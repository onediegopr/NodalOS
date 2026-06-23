# M672 Public Variant Reload Evidence Retry

Milestone: M672

Decision: HUMAN_ASSISTED_MANUAL_QA_HANDOFF_READY

This report defines the evidence templates for a real human-assisted reload of the public variant. The agent does not mark PASS without real visual or DevTools evidence.

Evidence to capture manually:

- Public variant loaded: true or false.
- Visible extension name.
- Manifest source confirmed as staged `manifest.public.json`.
- Permission warnings, if visible.
- Runtime tab status.
- Service Worker DevTools console status.
- CSP violations.
- `ERR_CONNECTION_REFUSED`.
- `invalid_token`.
- close 1008 or policy violation.
- reconnect storm.
- critical console errors.
- bridge liveness state.
- allowed local origins behavior.
- disallowed external origins behavior.

Redaction rule: do not paste API keys, bearer tokens, cookies, secrets, long raw logs, or private user data.

Public release remains NO-GO until real evidence is provided and reviewed.
