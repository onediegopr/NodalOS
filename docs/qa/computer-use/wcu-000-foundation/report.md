# WCU-000-007 Foundation Report

Date: 2026-06-26

Project: NODAL OS

Decision: `GO_WCU_FIXTURE_SAFE_FOUNDATION_READY`

Branch: `chrome-lab-001-extension-local-ai-bridge`

Initial HEAD: `ab741ed7261142925bee77bbea549a873d94314e`

Final HEAD: recorded in final operator response after commit. A commit cannot stably self-reference its own hash.

## Initial Guard

- Branch verified: `chrome-lab-001-extension-local-ai-bridge`
- Origin sync: `0 0`
- Worktree: clean
- Protected scope: no diff
- Existing Computer Use/UIA code exists in other modules, so WCU was created as a separate fixture-safe project and does not reuse live UIA executors.

## Implemented

- `src/OneBrain.WindowsComputerUse/`
- Fixture-safe contracts and builders.
- Capability classifier.
- Blockage and sensitive surface detectors.
- Locator scoring.
- Dry-run safe action planner.
- Evidence pack and redactor.
- Unit tests under `TestCategory=WindowsComputerUseFixtureSafe`.
- Architecture, threat model, gates, capability matrix, handoff, and next-prompt docs.

## No-Live Proof

- No live CDP.
- No browser live.
- No WebSocket live bridge.
- No Safe Injection live.
- No real Windows mouse.
- No real Windows keyboard.
- No live UIA/FlaUI action dependency.
- No clipboard capture.
- No raw screenshot persistence.
- No product UI action enablement.
- No Stealth Core changes.

## Validations

- `dotnet restore .\OneBrain.slnx`: PASS, required because the new project needed an assets file before no-restore build.
- `dotnet build .\OneBrain.slnx --no-restore`: PASS.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseFixtureSafe`: PASS, 12/12.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=CloakBrowserPerceptionRouter`: PASS, 83/83.
- `git diff --check`: PASS.
- JSON validation for `report.json`: PASS.
- Protected scope scan: PASS, no diff.
- Allowed paths scan: PASS.
- Secret scan changed/new: PASS with allowed fake redaction-test literals only.
- No-live usage scan changed/new code: PASS.
- Bad UX wording scan changed/new: PASS after wording cleanup.

## Percentages

- WCU fixture-safe foundation: 100%
- WCU observability design readiness: 40%
- WCU UIA live read-only readiness: 5%
- WCU controlled action readiness: 0%
- WCU product automation readiness: 0%

## Risks

- UIA live read-only adapter remains unimplemented.
- Any live PC control requires a new human decision, ADR, prompt, threat model, gates, and audit.
- Existing repo has older UIA execution modules; WCU must remain isolated unless a future prompt explicitly authorizes a bridge.

## Next Block

`WCU-008/009 — UIA READ-ONLY ADAPTER DESIGN`, design-only and no live actions.
