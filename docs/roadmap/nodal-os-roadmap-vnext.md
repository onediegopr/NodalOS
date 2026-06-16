# NODAL OS Roadmap vNext

## Current State

- Product name: NODAL OS.
- Historical technical names still present: NEXA, ONE BRAIN.
- Visible rename NEXA to NODAL OS: completed in M97-M99.
- Technical `Nexa*` symbol cleanup: pending compatibility task.
- NODAL OS engineering: 97%.
- Browser Runtime local/sandbox: 97%.
- External HTTP read-only proof readiness: 90-95%.
- External target-owned Chrome/CDP/DOM read-only proof readiness: 85-90%.
- Security/evidence integrity: 92-95%.
- M51: closed with strict HTTP read-only scope.
- M65: closed with limited target-owned Chrome/CDP/DOM read-only scope.
- External Chrome/CDP/DOM proof: completed only for `https://lab.nodalos.com.ar`.
- External CDP general-ready: false.

## M51 Scope

M51 is closed only for:

- external HTTP read-only proof;
- target `https://lab.nodalos.com.ar`;
- `ProbeKind=RealHttpClient`;
- `Tooling=HttpReadOnlyExternal`;
- capabilities `HttpGetReadOnly`, `NetworkMetadataOnly`, `CoreGoverned`;
- redacted evidence persisted to `BrowserPersistentAuditLedger`.

M51 does not prove:

- Chrome/CDP external runtime;
- DOM read-only external proof;
- profile/browser process cleanup against external live target;
- auth target readiness;
- document workflow readiness;
- sensitive site readiness.

## M65 Status

M65 is formally closed only for:

- target-owned external low-risk Chrome/CDP/DOM read-only proof;
- target `https://lab.nodalos.com.ar`;
- isolated temporary browser profile;
- `ProbeKind=RealChromeCdp`;
- `Tooling=ChromeCdpExternalReadOnly`;
- redacted evidence persisted to `BrowserPersistentAuditLedger`;
- `LedgerRef=audit-ledger-edb3e2fbb0a0446788dae17a269c0058`;
- `LedgerHash=61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`;
- no credentials, no login real, no submit, no payment, no mutation, no sensitive site.

M65 does not mean external CDP general-ready.

M65 does not unlock third-party sites, sensitive sites, real credentials, submit/pay/sign/delete, production external CDP, SaaS public, public API, billing real, or email real.

## Recommended Next Blocks

### M97/M98/M99: Visible Rename NEXA to NODAL OS

Goal:

- Rename visible product surfaces from historical NEXA to NODAL OS where appropriate.
- Keep compatibility aliases if required.
- Do not mix rename with proof/security-critical changes.

Status:

- Completed for visible/operator-facing surfaces.
- Technical symbol cleanup remains future work.

Rules:

- Start from canonical worktree only.
- Preserve git diff with safeguard patch before commit.
- Validate full suite.

### M100/M101/M102: M65 Dedicated Evidence Plan

Goal:

- Define M65-specific evidence, scenarios, gates, and ledger requirements.
- Keep M65 deferred until this evidence exists.
- Do not use real credentials or sensitive sites.

### M103/M104/M105: External Chrome/CDP/DOM Read-Only Proof

Goal:

- Prove real browser runtime against the test-owned external target if still required.
- Use Chrome/CDP with controlled profile and Core authority.
- Persist evidence to HMAC ledger.
- Do not infer from HttpClient proof.

### M115/M116/M117: Product/Admin Private Preview Hardening

Goal:

- Harden Product/Admin private preview after M51 and M65 limited external evidence.
- Keep local/private authority boundaries.
- Keep SaaS public, public API, billing real, email real, and real credentials blocked.

### M118/M119/M120: Core Audit / External Proof Audit / Release Gate

Goal:

- Audit M51 HTTP evidence and M65 target-owned Chrome/CDP evidence.
- Verify ledger references, redaction and scope locks.
- Decide whether release gates need independent review before broader preview.

### M121/M122/M123: HITO-162 Rewrite / Map

Goal:

- Re-audit the legacy HITO-162 intent.
- Map it to the NODAL OS roadmap or rewrite it as a new block.
- Do not resume it blindly.

### M124+: Embedded Runtime Evaluation If Needed

Goal:

- Evaluate WebView2/CEF/embedded runtime only if a concrete limitation justifies it.
- Chromium fork is not planned unless a hard limitation appears.

### Legacy HITO-162 Reconciliation / Rewrite

Goal:

- Re-audit the legacy HITO-162 intent.
- Decide whether it maps to perception robustness, safe action expansion, or a new NODAL OS hito block.
- Do not resume it blindly.

### Product/Admin Private Preview Hardening

Continue local-only operator readiness, issue triage, private local API, diagnostics, support, and audit hardening.

### SaaS/API/Billing/Email Future Phases

Remain blocked until dedicated phases exist:

- public SaaS;
- public API;
- billing real;
- email real;
- real customer credentials.

## HITO-162 Decision

HITO-162 is paused/not forgotten.

It must be treated as a legacy milestone requiring reconciliation. It should be rewritten or mapped to the new NODAL OS roadmap using `docs/roadmap/nodal-os-legacy-hito-absorption-matrix.md`.

## Advancement Rules

- Use grouped milestones when they reduce coordination overhead.
- Do not mix rename with proof/security-critical changes.
- Do not close external/live broad capability without persisted ledger evidence.
- Do not treat M65 as external CDP general-ready.
- Do not open sensitive surfaces without dedicated evidence and gates.
- Keep Core authority: Core decides, Browser Runtime executes, UI/Companion/Admin observes/transports without authority.
- Keep percentages visible and honest.

## Active Restrictions

- No SaaS public.
- No public API real.
- No billing real.
- No email real.
- No real customer credentials.
- No sensitive sites.
- No AFIP, banks, ERP, fiscal, financial, or government sites.
- No submit/pay/sign/delete.
- No productive recorder/replay.
- No Chrome/CDP general-ready claim from target-owned proof.
- No Chromium fork planned now.

## M145-M147 Update

HITO-162 replacement is stable local fixture-first after M133-M144 and internal audit M145-M147.

Next phase recommendation:

- Product/Admin polish.
- Continue internal local private preview iteration.
- Run a focused Claude audit before broader local preview expansion if scope changes.
- Keep embedded runtime evaluation future-only.
- Keep Chromium fork not planned.

External CDP general-ready remains false. Production, SaaS public, public API real, billing/email real, credentials, sensitive sites, submit/pay/sign/delete, and productive recorder/replay remain blocked.
