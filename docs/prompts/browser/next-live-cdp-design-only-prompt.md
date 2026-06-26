# Next Prompt: Live CDP Design Only

Use this prompt only for design review. It does not authorize implementation.

## Mode

DESIGN-ONLY / READ-ONLY / NO IMPLEMENTATION.

Do not edit runtime code.
Do not implement live calls.
Do not add CDP calls.
Do not add WebSocket calls.
Do not add Safe Injection live.
Do not navigate external pages.
Do not execute browser actions.
Do not touch Stealth Core.
Do not touch protected scope.
Do not enable Mission Control productive actions.

## Context

NODAL OS closed the `CLOAK_BROWSER_PERCEPTION_ROUTER` line as fixture-safe:

- Browser perception snapshots.
- Page capability classifier.
- Strategy router.
- Locator engine.
- Blockage detector.
- Safe action planner.
- Browser action verifier.
- Controlled action executor V0 fixture-only.
- Browser evidence pack.
- Browser evidence redactor.

Audits:

- `KIMI_RE_AUDIT_GO`
- `RE_AUDIT_GO`

Fixture-safe closure decision:

- `SAFE_TO_CLOSE_FIXTURE_SAFE_LINE: YES`
- `SAFE_TO_START_LIVE_CDP_ACTIONS: NO`
- `SAFE_TO_START_LIVE_SAFE_INJECTION: NO`
- `SAFE_TO_ENABLE_PRODUCTIVE_BROWSER_AUTOMATION: NO`

## Objective

Draft or review a design-only plan for a future live read-only CDP collector.

The output must be a document only. It must not include code changes, runtime changes, bridge changes, live calls, tests that navigate, or executable interfaces.

## Required Design Topics

1. Read-only collector boundary.
2. CDP data allowed for metadata summaries.
3. CDP data prohibited from collection.
4. Redaction requirements.
5. Human handoff triggers.
6. Live-disabled default.
7. No action execution.
8. Protected scope boundaries.
9. No system browser fallback.
10. No Chrome Extension default fallback.
11. Audit requirements before implementation.

## Explicit NO-GO

- Live click.
- Live type.
- Live select.
- Live scroll.
- Live wait.
- Safe Injection live.
- WebSocket action bridge.
- Mission Control action enablement.
- CAPTCHA solving.
- 2FA bypass.
- Anti-bot bypass.
- Login automation without explicit user action.
- Paywall bypass.
- Credential entry.

## Expected Output

Return only a design document or audit report.

End with:

- `LIVE_CDP_DESIGN_ONLY: YES`
- `LIVE_CDP_IMPLEMENTATION_ALLOWED: NO`
- `SAFE_INJECTION_LIVE_ALLOWED: NO`
- `PRODUCTIVE_BROWSER_AUTOMATION_ALLOWED: NO`
