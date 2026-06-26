# Browser Automation Capability Matrix

Date: 2026-06-26

Project: NODAL OS

Line: `CLOAK_BROWSER_PERCEPTION_ROUTER`

Status: fixture-safe line closed; live capabilities remain gated.

## A. Fixture-Safe Capabilities Closed

These capabilities are implemented only over fixtures, snapshots, metadata, and in-memory state.

| Capability | Status | Boundary |
| --- | --- | --- |
| Perceive fixture snapshot | Closed fixture-safe | Metadata-only `BrowserPerceptionSnapshot` |
| Classify page | Closed fixture-safe | Deterministic classifier over snapshot metadata |
| Select locator strategy | Closed fixture-safe | Candidate-only locator strategy |
| Detect blockage from metadata | Closed fixture-safe | No bypass, no dismissal, no mutation |
| Plan safe actions | Closed fixture-safe | Theoretical plans only |
| Verify pre/post snapshots | Closed fixture-safe | Snapshot comparison only |
| Execute in-memory fixture actions | Closed fixture-safe | `FixturePageState` only |
| Generate evidence pack | Closed fixture-safe | Structured metadata evidence |
| Redact secrets | Closed fixture-safe | Defensive field and pattern redaction |

## B. Design-Only Future Capabilities

These capabilities may be discussed as design, but must not be implemented without a new approved gate.

| Capability | Status | Required Gate |
| --- | --- | --- |
| Live CDP snapshot collector | Design-only | New ADR, threat model, tests, audit |
| Live DOM/AX/layout/screenshot collector | Design-only | Read-only collector gate |
| Live no-action diagnosis | Design-only | Read-only diagnosis gate |
| Live read-only console/network summary | Design-only | Redaction and no-action audit |
| Live locator validation | Design-only | No-action validation gate |
| Live precondition check | Design-only | Explicit disabled-by-default gate |

## C. Future Gated Capabilities Requiring New Approval

These capabilities are not allowed by the fixture-safe closure.

| Capability | Current Status | Minimum Future Requirement |
| --- | --- | --- |
| Live click | Blocked | Human approval, allowlist, evidence, audit |
| Live type | Blocked | Sensitive input guard, explicit user action, audit |
| Live select | Blocked | Target validation, evidence, audit |
| Live scroll | Blocked | Explicit live action gate, evidence, audit |
| Live wait | Blocked | Policy and timeout model, audit |
| Safe Injection live | Blocked | Separate ADR, threat model, redaction proof |
| WebSocket action bridge | Blocked | Protected boundary approval and audit |
| Mission Control action enablement | Blocked | Product gate, user affordance review, audit |

## D. Permanently Prohibited Or Human-Handoff

The following must not become automated behavior in NODAL OS.

- CAPTCHA solving.
- 2FA bypass.
- Anti-bot bypass.
- Login without user authorization.
- Paywall bypass.
- Credential entry without explicit user action.
- Hidden automation over protected sites.
- System browser fallback.
- Chrome Extension fallback as default.

For these cases the allowed route is human handoff, explicit user decision, or no action.

## Future Live Definition Of Done

Any future live phase must satisfy all items below before implementation is allowed:

1. New human decision.
2. New ADR.
3. New prompt.
4. New threat model.
5. New protected scope scan.
6. New no-bypass proof.
7. New redaction proof.
8. New human handoff tests.
9. New live-disabled default.
10. External audit before enabling.

## Closure Decision

- Fixture-safe line: closed.
- Live CDP actions: blocked.
- Safe Injection live: blocked.
- Productive browser automation: blocked.
