# ADR: Browser Runtime Sensitive Automation Checkpoint M35A

## Status

Accepted.

## Context

M35A is a checkpoint after M17 through M34A. It does not enable new automation capabilities. It audits the Browser Runtime and sensitive automation roadmap before any real sensitive pilot.

Current state:

- M17 hardened audit ledger integrity, HMAC/head seal, redaction, and metadata-only network capture.
- M18 proved CDP live read-only against local fixtures.
- M19 moved the phase gate from caller-provided flags to observed runtime state.
- M20 designed productive vault architecture without enabling productive storage.
- M21/M22 added consent UI/surface and controlled profile activation.
- M23/M24 added minimal sandbox vault and authenticated local sandbox flow.
- M25 remains contract/policy ready, but M25B is still blocked because no external test-owned low-risk target exists.
- M26/M27 closed safe download/upload real against controlled local/low-risk fixtures.
- M28A closed document workflow end-to-end local/sandbox.
- M29 designed recorder safely.
- M30/M31 added read-only recorder prototype and replay safe mode.
- M32 added compliance and sensitive sites policy.
- M33A/M34A added sensitive read-only and sensitive document simulations only.

## What Is Productive Enough

- Core-governed CDP read-only runtime.
- Metadata-only network capture.
- HMAC/head-sealed audit ledger.
- Safe download and safe upload policy layers.
- Consent surface and controlled profile activation contracts.
- Runtime phase gate from observed state.
- Sensitive site policy evaluator.
- Local/sandbox sensitive simulations.

## What Remains Sandbox Or Simulation

- Vault minimal remains sandbox/dev-safe, not final productive OS-backed storage.
- Authenticated flows remain local/sandbox or blocked for external live target.
- Sensitive read-only and sensitive document flows are simulations.
- Recorder remains read-only prototype.
- Replay remains safe-mode read-only.

## What Remains Blocked

- AFIP, banks, ERP, fiscal, financial, government, healthcare, legal, customer-data real sites.
- Real sensitive pilots M33B/M34B.
- M28 external real workflow.
- Real external low-risk auth until M25B has a test-owned target.
- Submit, pay, sign, delete, publish, approve.
- Productive recorder and productive replay.
- Raw personal Chrome profile.
- Real customer/commercial/personal credentials.
- Request/response bodies and sensitive header values.

## Risk Register

- R1: M25B has no external test-owned target.
- R2: No real sensitive pilot has been run.
- R3: Vault minimal is sandbox/dev-safe, not final productive vault.
- R4: DPAPI/Windows Credential Manager provider is not implemented.
- R5: WebView2/CEF are not implemented.
- R6: Product/admin/licensing controls are not integrated.
- R7: Recorder/replay are safe/read-only, not productive.
- R8: Sensitive sites require compliance/legal/operations approvals.
- R9: Submit/pay/sign/delete remain blocked.
- R10: M28 external workflow remains blocked.

## Decision Matrix

M25B external test-owned target:
Advance with conditions. It is the most direct blocker for external validation. Requires test-owned target, synthetic credentials, allowlist, no sensitive data, and opt-in live tests.

M33B sensitive read-only real pilot:
Do not advance now. Requires M25B, compliance/legal/operations approval, explicit pilot decision, and no irreversible actions.

M34B sensitive document real pilot:
Do not advance now. Requires M33B readiness plus document handling policy and independent audit of safe download/upload evidence.

M35 Critical Submit Gate:
Advance with conditions if the next goal is irreversible-action governance. It must not enable submit/pay/sign/delete automatically.

Admin/Licensing/Product track:
Advance with conditions. This can progress without touching real sensitive sites and is useful for tenant policy, admin controls, and productization.

WebView2/CEF architecture:
Advance with conditions only as architecture/design. It must preserve Core authority and companion non-authority.

Productive Vault OS-backed implementation:
Advance with conditions. It needs DPAPI/Credential Manager choice, key custody policy, and audit review before any real secret use.

## Recommendation

Recommended next path:

1. M25B external test-owned target if the objective is external validation.
2. Admin/Licensing/Product track if the objective is product readiness without sensitive-site risk.
3. Productive Vault OS-backed implementation if the objective is secret custody hardening.

Do not advance to M33B/M34B real sensitive pilots until M25B is unblocked and a formal pilot decision is approved.
