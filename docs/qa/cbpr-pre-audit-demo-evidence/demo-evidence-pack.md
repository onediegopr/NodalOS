# CBPR Pre-Audit Demo Evidence Pack

Date: 2026-06-26

Project: NODAL OS

Scope: fixture-safe demo evidence only. No live browser, no external navigation, no real actions.

## Demo Summary

| Scenario | Input Fixture | Expected Decision | Evidence | No-Live Proof | Redaction | Status |
| --- | --- | --- | --- | --- | --- | --- |
| `DEMO-001-plan-only` | legacy form fixture | plan only, DOM/CSS strategy | plan-only evidence | no CDP, no WebSocket, no browser launch | no sensitive input | PASS |
| `DEMO-002-blockage` | CAPTCHA marker fixture | human handoff | blockage evidence | no solver, no bypass | captcha marker category only | PASS |
| `DEMO-003-locator` | SPA semantic fixture | accessibility locator selected | locator candidate evidence | candidate only, no execution | locator metadata redacted | PASS |
| `DEMO-004-human-handoff` | 2FA/auth fixture | human handoff required | handoff evidence | no credential/challenge automation | auth/challenge category only | PASS |
| `DEMO-005-plan-not-live` | safe action plan fixture | safe action planned but not live | action plan evidence | `ProhibitedOnExternalPages=true` | no secret | PASS |
| `DEMO-006-fixture-success` | fixture page state | fixture action succeeded | fixture execution success | in-memory only | no secret | PASS |
| `DEMO-007-fixture-failure` | fixture postcondition mismatch | fixture execution failed | fixture failure evidence | in-memory only | no secret | PASS |
| `DEMO-008-verification-failure` | missing target fixture | precondition failure | verification failure evidence | action not attempted | no secret | PASS |
| `DEMO-009-redaction-adversarial` | secret-like fixture metadata | redacted evidence | no live capture | password/token/key/session patterns redacted | PASS |

## Scenario Details

### `DEMO-001-plan-only`

- Input fixture: legacy HTML form metadata.
- Expected decision: `DOM_FIRST` or CSS locator strategy.
- Expected evidence: `BrowserEvidenceKind.PlanOnly`.
- No-live proof: no CDP invoked, no WebSocket invoked, no browser launched, no external navigation, no real action.
- Redaction expectation: `RedactionStatus=None`.
- Test reference: `CloakBrowserPerceptionRouterFoundationTests`, `CloakBrowserSafeActionPlannerTests`.
- Status: PASS.

### `DEMO-002-blockage`

- Input fixture: CAPTCHA/human verification marker.
- Expected decision: `HUMAN_HANDOFF_REQUIRED`.
- Expected evidence: blockage or human handoff evidence with `HumanHandoffTriggered=true`.
- No-live proof: no CAPTCHA solving, no bypass, no action attempted.
- Redaction expectation: challenge category only; no raw sensitive payload.
- Test reference: `CloakBrowserLocatorBlockageTests`, `CloakBrowserEvidencePackTests`.
- Status: PASS.

### `DEMO-003-locator`

- Input fixture: SPA semantic interactive elements.
- Expected decision: accessibility locator strategy.
- Expected evidence: locator candidate metadata.
- No-live proof: locator is candidate-only and does not execute.
- Redaction expectation: secret-like locator metadata is replaced with `[REDACTED]`.
- Test reference: `CloakBrowserLocatorBlockageTests`.
- Status: PASS.

### `DEMO-004-human-handoff`

- Input fixture: auth/2FA metadata.
- Expected decision: human handoff.
- Expected evidence: handoff evidence and blockage reason.
- No-live proof: no login automation, no OTP handling, no credential entry.
- Redaction expectation: no credential/session values.
- Test reference: `CloakBrowserSafeActionPlannerTests`, `CloakBrowserEvidencePackTests`.
- Status: PASS.

### `DEMO-005-plan-not-live`

- Input fixture: safe action objective over fixture snapshot.
- Expected decision: theoretical action plan only.
- Expected evidence: plan metadata with `CanExecuteInFixtureOnly=true`.
- No-live proof: `ProhibitedOnExternalPages=true`; no live executor.
- Redaction expectation: no sensitive input.
- Test reference: `CloakBrowserSafeActionPlannerTests`.
- Status: PASS.

### `DEMO-006-fixture-success`

- Input fixture: visible enabled target in `FixturePageState`.
- Expected decision: fixture action succeeds.
- Expected evidence: `FixtureExecutionSucceeded`.
- No-live proof: only in-memory fixture state changes.
- Redaction expectation: `RedactionStatus=None`.
- Test reference: `CloakBrowserControlledActionExecutorTests`, `CloakBrowserEvidencePackTests`.
- Status: PASS.

### `DEMO-007-fixture-failure`

- Input fixture: postcondition mismatch.
- Expected decision: fixture execution failed.
- Expected evidence: `FixtureExecutionFailed` or `VerificationFailed`.
- No-live proof: no retry against live browser.
- Redaction expectation: no sensitive input.
- Test reference: `CloakBrowserControlledActionExecutorTests`, `CloakBrowserEvidencePackTests`.
- Status: PASS.

### `DEMO-008-verification-failure`

- Input fixture: missing target or failed precondition.
- Expected decision: action aborted before fixture mutation.
- Expected evidence: verification failure with failed preconditions.
- No-live proof: `Attempted=false` or aborted before side effects.
- Redaction expectation: no sensitive input.
- Test reference: `CloakBrowserControlledActionExecutorTests`.
- Status: PASS.

### `DEMO-009-redaction-adversarial`

- Input fixture: field names and values that look like password, token, API key, session, cookie, OTP, 2FA, CAPTCHA, JWT, or long opaque secret.
- Expected decision: evidence redacts sensitive values.
- Expected evidence: `SensitiveFieldsRedacted` lists categories or field names, not values.
- No-live proof: fixture string only; no live capture.
- Redaction expectation: serialized JSON does not contain raw sensitive values.
- Test reference: `CloakBrowserEvidencePackTests`, `CloakBrowserControlledActionExecutorTests`, `CloakBrowserLocatorBlockageTests`.
- Status: PASS.

## Decision

`PRE_AUDIT_DEMO_EVIDENCE_PACK_READY`

The demos prove fixture-safe behavior and do not claim live readiness.
