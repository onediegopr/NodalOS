# Audit IA 2 Prompt: Technical No-Live Verification

Mode: AUDIT-ONLY / READ-ONLY / NO MODIFICATIONS.

Do not implement changes.
Do not edit files.
Do not refactor.
Do not commit.
Do not push.
Do not apply patches.

## Project

NODAL OS

Branch: `chrome-lab-001-extension-local-ai-bridge`

Line: `CLOAK_BROWSER_PERCEPTION_ROUTER`

## Audit Focus

Review git evidence, tests, scans, no-live proof, protected paths, forbidden browser usage, runtime changes, fixture-safe boundaries, and wording risk.

## Required Git Checks

Run or inspect equivalent:

- `git status --short`
- `git branch --show-current`
- `git rev-parse HEAD`
- `git rev-list --left-right --count HEAD...origin/chrome-lab-001-extension-local-ai-bridge`
- protected scope diff
- changed/new files list

## Protected Scope

Any diff in these paths is P0 and NO-GO:

- `stealth-engine/src/evasion/**`
- `stealth-engine/src/captcha/**`
- `stealth-engine/src/fingerprint/**`
- `stealth-engine/src/behavior/**`
- `stealth-engine/src/proxy/**`
- `stealth-engine/src/antiBlocking/**`
- `stealth-engine/src/handoff/**`
- `stealth-engine/src/StealthSession.js`
- `stealth-engine/src/StealthBrowserManager.js`
- `stealth-engine/src/index.js`
- `stealth-engine/tests/stealth-suite.test.js`

## Commands To Run If Environment Allows

- `dotnet build .\OneBrain.slnx --no-restore`
- `dotnet test .\OneBrain.slnx --filter TestCategory=CloakBrowserPerceptionRouter`
- `git diff --check`
- `git diff --cached --check`
- secret scan changed/new
- forbidden browser usage scan changed/new
- bad UX wording scan changed/new
- JSON validation

## Search Targets

Check changed/new files and relevant CBPR source for:

- live CDP calls.
- WebSocket live bridge.
- Safe Injection live.
- external navigation.
- real browser action APIs.
- product UI action enablement.
- runtime changes.
- system browser fallback.
- extension default fallback.
- credential or challenge bypass.

## No-Live Expected Result

The audit should confirm:

- CDP live: NO
- WebSocket live: NO
- browser launch: NO
- system browser fallback: NO
- extension default fallback: NO
- external navigation: NO
- real browser actions: NO
- Safe Injection live: NO
- product UI browser action enablement: NO
- Stealth Core changes: NO

## Severity Rules

P0:

- Protected scope modified.
- Live CDP, WebSocket live bridge, Safe Injection live, external navigation, or real action introduced.
- System browser fallback or extension default fallback introduced.
- Credential/challenge bypass introduced.

P1:

- Missing no-live proof.
- Tests fail or are skipped without explanation.
- Scans are missing or ambiguous.
- Docs claim live readiness.

P2:

- Scan coverage incomplete.
- JSON or reports inconsistent.
- Fixture-safe claims not fully traceable to tests.

P3:

- Minor wording issues.
- Formatting clarity.

## Required Output

Return:

1. Verdict: `GO`, `CONDITIONAL_GO`, or `NO_GO`.
2. Executive summary.
3. Git state.
4. Protected scope result.
5. Build/test result.
6. Scan result.
7. No-live proof result.
8. Findings table with P0/P1/P2/P3.
9. Required corrections if any.
10. Final decisions:
    - `SAFE_TO_CLOSE_PRE_AUDIT_STAGE: YES/NO`
    - `SAFE_TO_START_LIVE_IMPLEMENTATION: NO`
    - `SAFE_TO_REQUEST_HUMAN_LIVE_DESIGN_DECISION: YES/NO`

Do not request or perform live implementation.
