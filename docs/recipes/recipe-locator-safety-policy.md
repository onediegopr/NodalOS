# Recipe Locator Safety Policy

Phase: 7/9 - Recipe Lab + Locator Repair Studio.

Locator repair is a review aid, not an execution path.

## Policy

- `RelativeCoordinate` is last-resort and requires manual review.
- `AIFallback` is blocked for sensitive actions unless a future policy and human path explicitly allow review.
- `HumanHandoff` is a safe fallback.
- Missing locator evidence blocks repair decisions.
- Unsafe, broken or ambiguous locator states do not become executable actions.
- Replay eligibility never means live replay in Phase 7.

## Prohibited

- Real browser selector testing.
- Real desktop observation.
- Real DOM/accessibility/screenshot/HAR capture.
- Live locator repair apply.
- Recorder/replay.
- Automatic recipe or workitem execution.
