# Browser Automation Capability Matrix v2

Date: 2026-06-26

Project: NODAL OS

Decision: `CAPABILITY_MATRIX_V2_READY_FOR_PRE_AUDIT`

This matrix separates fixture-safe capability from live-readiness. Fixture-safe does not mean live-ready.

| Capability | Current Status | Type | Evidence | Tests | Risks | Unlock Requirements | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Perception snapshot | Implemented | fixture-safe | `BrowserPerceptionSnapshot` | CBPR tests | Metadata could be over-trusted | Live collector ADR and audit | Closed fixture-safe |
| Page capability classification | Implemented | fixture-safe | `PageCapabilityClassifier` | CBPR tests | Low confidence ambiguity | Human handoff remains dominant | Closed fixture-safe |
| Technology profile | Implemented | fixture-safe | `PageTechnologyProfile` | CBPR tests | Misclassification | Confidence threshold and audit | Closed fixture-safe |
| Strategy router | Implemented | fixture-safe | `StrategyRouterDecision` | CBPR tests | Wrong strategy if blockage missed | Blockage first, handoff priority | Closed fixture-safe |
| Locator strategy | Implemented | fixture-safe | `LocatorEngine` | CBPR tests | Selector spoofing | Read-only live validation gate | Closed fixture-safe |
| Blockage detection | Implemented | fixture-safe | `BlockageDetector` | CBPR tests | Challenge false negatives | No-bypass tests before live | Closed fixture-safe |
| Safe action plan | Implemented | fixture-safe | `SafeActionPlanner` | CBPR tests | Objective text tricking planner | Sensitive/bypass guard audit | Closed fixture-safe |
| Browser action verifier | Implemented | fixture-safe | `BrowserActionVerifier` | CBPR tests | Snapshot mismatch | Live verification design | Closed fixture-safe |
| Controlled executor fixture-only | Implemented | fixture-safe | `ControlledActionExecutor` | CBPR tests | Mistaken for live executor | Live modes abort, docs warning | Closed fixture-safe |
| Evidence pack | Implemented | fixture-safe | `BrowserEvidencePack` | CBPR tests | Secret leakage | Redaction audit before live | Closed fixture-safe |
| Redaction | Implemented | fixture-safe | `BrowserEvidenceRedactor` | CBPR tests | Pattern gaps | Live-summary audit | Closed fixture-safe |
| Human handoff | Implemented | fixture-safe | strategy/planner/blockage decisions | CBPR tests | Product copy ambiguity | UX review before live | Closed fixture-safe |
| Live CDP action | Not implemented | disabled | no live action code | no-live scans | Accidental real action | New decision, ADR, tests, audit | `NO_GO_FOR_LIVE` |
| Live Safe Injection | Not implemented | disabled | no live injection code | no-live scans | Mutation/exfiltration | Separate ADR, threat model, audit | `NO_GO_FOR_LIVE` |
| External navigation | Not implemented | disabled | no navigation code | no-live scans | Unauthorized site access | Domain allowlist and approval | `NO_GO_FOR_LIVE` |
| Product Mission Control browser action | Not implemented | disabled | no product UI enablement | no-live scans | User misunderstanding | Product gate and audit | `NO_GO_FOR_LIVE` |
| Login flow | Not automated | forbidden/handoff | handoff docs/tests | CBPR tests | Credential/session risk | Explicit user action only | Human handoff |
| CAPTCHA/2FA/challenge handling | Not automated | forbidden/handoff | handoff docs/tests | CBPR tests | Bypass risk | Never automate solving | Human handoff |
| System browser fallback | Not allowed | forbidden | prior runtime gates | migration tests | Runtime drift | Pinned CloakBrowser only | Prohibited |
| Extension default fallback | Not allowed | forbidden | extension legacy/no-default | no-extension tests | Default path regression | No-extension default proof | Prohibited |

## Current Capability Percentages

- CloakBrowser runtime base: 100%
- CBPR fixture-safe: 100%
- Perception Router: 72%
- Browser diagnosis: 67%
- Locator Engine: 50%
- Blockage Detector: 60%
- Safe actions fixture-safe: 55%
- Governance / threat model readiness: 90%
- Pre-live design readiness: 38%
- Browser automation productive: 0%
- Live implementation readiness: 0%

The 72% Perception Router and 67% Browser diagnosis values include documentation and governance hardening after the technical fixture-safe close. They do not represent live readiness.

## Decision

- Fixture-safe capabilities are closed.
- Design-only future capabilities may be reviewed.
- Future-live capabilities require new approval.
- Forbidden capabilities remain blocked or human-handoff only.
