# NODAL OS Roadmap vNext

## Current State

- Product name: NODAL OS.
- Historical technical names still present: NEXA, ONE BRAIN.
- Visible rename NEXA to NODAL OS: completed in M97-M99.
- Technical `Nexa*` symbol cleanup: pending compatibility task.
- NODAL OS engineering: 97%.
- Browser Runtime local/sandbox: 97%.
- External HTTP read-only proof readiness: 90-95%.
- Security/evidence integrity: 92-95%.
- M51: closed with strict HTTP read-only scope.
- M65: deferred, `DeferredNeedsDedicatedEvidence`.
- External Chrome/CDP/DOM proof: pending.

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

M65 remains deferred.

Required next evidence for M65:

- dedicated external low-risk/auth target plan;
- explicit allowed and denied scenario matrix;
- proof not derived from M51 HTTP-only evidence;
- redacted evidence ledger;
- no credentials, no login real, no submit, no payment, no sensitive site.

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

### M106+: Legacy HITO-162 Reconciliation / Rewrite

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
- No Chrome/CDP external claim until Chrome/CDP external evidence exists.
