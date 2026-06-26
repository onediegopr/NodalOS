# Browser Automation Threat Model v1

Date: 2026-06-26

Project: NODAL OS

Line: `CLOAK_BROWSER_PERCEPTION_ROUTER`

Decision: `THREAT_MODEL_READY_FOR_PRE_LIVE_DESIGN_REVIEW`

Scope: design-only, fixture-safe, governance-only. This document does not authorize live CDP, live WebSocket, Safe Injection live, external navigation, real browser actions, productive automation, or Stealth Core changes.

## Protected Assets

- User credentials, sessions, cookies, tokens, API keys, OTP, 2FA material, and recovery answers.
- Local filesystem paths, project source files, private repository contents, and local artifacts.
- CloakBrowser runtime pinning, lockfile metadata, and no-system-browser guarantees.
- Stealth Core audited behavior and protected browser isolation assumptions.
- Browser perception evidence, redacted snapshots, fixture state, and audit reports.
- Human intent, human approval state, and future action authorization state.
- Product trust: UI wording must not imply live capabilities that are not enabled.

## Attack Surfaces

- Future live CDP collector.
- Future live action gateway.
- Future Safe Injection layer.
- Future WebSocket action bridge.
- Future Mission Control integration.
- Future user approval UX.
- Browser page DOM, accessibility tree, frames, shadow DOM, canvas, console, network summaries, and screenshot metadata.
- Evidence pack serialization and downstream display.
- Test fixtures and demo evidence.
- Prompt or page content that attempts to influence automation decisions.

## Trust Boundaries

| Boundary | Trusted Side | Untrusted Or Lower-Trust Side | Required Control |
| --- | --- | --- | --- |
| Fixture-safe CBPR modules | NODAL OS code and tests | Fixture inputs | Redaction, deterministic tests, no live calls |
| Browser page to snapshot | Future collector | Page DOM and content | Read-only, redaction, no prompt trust |
| Snapshot to classifier/router | CBPR contracts | Snapshot metadata | Human handoff for uncertainty |
| Planner to executor | Fixture-only executor | Action objective text | Sensitive input and bypass guards |
| Evidence to product display | Redacted evidence | Raw captured data | Metadata-only evidence, no secrets |
| Future UI to live gateway | Explicit human approval | Product affordance text | Disabled by default, allowlist, audit |
| Runtime boundary | Pinned CloakBrowser | System browser and extension fallback | No fallback gates |

## Risks By Layer

### CloakBrowser Runtime

- Risk: system browser fallback.
- Risk: unpinned runtime drift.
- Risk: extension fallback becomes default again.
- Current mitigation: runtime pinning and no-extension default harness from prior migration.
- Before live: re-run runtime gates and audit runtime path/hash.

### CBPR

- Risk: fixture-safe APIs are mistaken for live-ready APIs.
- Risk: low confidence classification still leads to action planning.
- Current mitigation: human handoff for low confidence and blockage conditions.
- Before live: live-disabled default must be enforced by executable gates.

### Locator

- Risk: selector spoofing or wrong target.
- Risk: locator metadata leaks sensitive text.
- Current mitigation: candidates only; no live execution; locator redaction.
- Before live: locator validation must be read-only first and require evidence.

### Blockage Detector

- Risk: CAPTCHA, 2FA, anti-bot, or login treated as resolvable.
- Current mitigation: human handoff dominates strategy and planner.
- Before live: negative tests must prove no bypass and no automatic challenge handling.

### Safe Action Planner

- Risk: objective text requests credential entry, bypass, or destructive actions.
- Current mitigation: sensitive and bypass terms route to human handoff.
- Before live: approval UX and action allowlist required.

### Evidence Pack

- Risk: sensitive values leak through summaries, refs, or JSON serialization.
- Current mitigation: defensive field and pattern redaction; redaction metadata.
- Before live: live-summary redaction audit required.

### Future Live CDP

- Risk: accidental navigation, mutation, or action.
- Risk: page content prompt injection influences action policy.
- Required mitigation: separate read-only collector, no action gateway in collector, disabled-by-default live gate.

### Future Safe Injection

- Risk: injected code mutates page or exfiltrates data.
- Required mitigation: separate ADR, threat model, allowlist, no credential/challenge handling, and audit before implementation.

### Future Mission Control Integration

- Risk: product UI implies enabled browser actions.
- Risk: user misunderstands preview as execution.
- Required mitigation: explicit disabled state, review copy, approval gates, and evidence display.

### Future Collector

- Risk: screenshot leakage, DOM poisoning, storage leakage, network secret leakage.
- Required mitigation: metadata-only first, redaction, no storage values, no raw sensitive payloads.

### Future User Approval

- Risk: approval spoofing, stale approval reuse, broad approval, or replay.
- Required mitigation: scoped, time-bound, target-specific approval with audit log.

### Future Credential And Session Handling

- Risk: credential entry, session hijacking, token leakage, account lockouts.
- Required mitigation: no credential entry by automation; explicit user action and human handoff.

## Threats

| Threat | Severity | Probability Before Live | Current Fixture-Safe Mitigation | Required Before Live |
| --- | --- | --- | --- | --- |
| Unauthorized external navigation | Critical | Low | No live navigation exists | Allowlist, disabled default, tests |
| Accidental real actions | Critical | Low | Fixture-only executor | Action gateway approval and audit |
| Destructive clicks | Critical | Low | No live click exists | Action allowlist and postcondition verification |
| Sensitive forms | Critical | Medium | Planner/executor sensitive guards | Credential policy and handoff |
| Login, 2FA, CAPTCHA, challenge handling | Critical | Medium | Human handoff | No-bypass tests and UX |
| Anti-bot bypass | Critical | Low | No bypass logic | Explicit prohibition and audit |
| Data exfiltration | Critical | Medium | Metadata-only evidence and redaction | Live redaction audit |
| Prompt injection from web content | High | Medium | Fixture data is not trusted as policy | Policy isolation and tests |
| DOM poisoning | High | Medium | Deterministic classifier only | Read-only validation and evidence |
| Selector spoofing | High | Medium | Candidate-only locators | Locator validation gate |
| Screenshot leakage | High | Medium | Screenshot metadata only | Redaction and storage policy |
| Token/session leakage | Critical | Medium | Redactor patterns | Live capture restrictions |
| Filesystem leakage | High | Low | No product file writes | Sandbox and path redaction |
| System browser fallback | Critical | Low | Prior no-system-browser gates | Runtime proof on every live block |
| Chrome Extension default fallback | High | Low | Extension legacy/no-default | No-extension default proof |
| UI wording overclaims capability | Medium | Medium | Docs state blocked status | Product copy review |

## Severity And Probability Matrix

| Probability / Severity | Low | Medium | High | Critical |
| --- | --- | --- | --- | --- |
| Low | Monitor | Track | Gate before live | Block until audited |
| Medium | Track | Mitigate | Block until mitigated | Block until audited |
| High | Mitigate | Block | Block | Block |

Any Critical threat with Medium or higher probability is `NO_GO_FOR_LIVE`.

## Current Fixture-Safe Mitigations

- No live CDP implementation.
- No WebSocket live bridge.
- No Safe Injection live.
- No external navigation.
- No real browser actions.
- No product UI action enablement.
- Fixture-only executor.
- Human handoff on CAPTCHA, 2FA, anti-bot, login, low confidence, and sensitive input.
- Evidence redaction for field names and secret-like patterns.
- No protected Stealth Core changes.
- No system browser fallback.
- No extension default fallback.

## Mandatory Mitigations Before Live

- New human decision.
- New ADR.
- New threat model delta.
- New no-live disabled-by-default gates.
- Target allowlist.
- Action allowlist.
- Human approval UX.
- Kill switch.
- Audit log.
- Redaction proof over live summaries.
- No-bypass tests.
- Protected scope scan.
- External audit.

## Decision

`THREAT_MODEL_READY_FOR_PRE_LIVE_DESIGN_REVIEW`

This means the threat model is ready for auditors to review. It does not mean live CDP, Safe Injection live, real actions, external navigation, or productive browser automation are ready.
