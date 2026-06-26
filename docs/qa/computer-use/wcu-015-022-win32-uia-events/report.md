# WCU-015-022 Win32 Context + UIA Events Read-Only Fixture Interop

Decision: `GO_WCU_WIN32_CONTEXT_UIA_EVENTS_READ_ONLY_FIXTURE_READY`

## Scope

This block added fixture-safe Win32 context contracts, UIA event stream contracts, evidence/redaction support, and fusion integration. It did not add P/Invoke, FlaUI, live UIA subscription, real PC reads by default, input injection, window manipulation, clipboard access, screenshot capture, browser live, CDP live, WebSocket live, or OCR provider execution.

## Git Baseline

- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial expected HEAD: `9072c3ec34e3fcd5eb9da9e70d13b467deabb880`
- Initial actual HEAD at resumed guard: `0242d53f2f8fb343782d9387a9b8ea4f0e0d81be`
- Note: expected base `9072c3ec34e3fcd5eb9da9e70d13b467deabb880` is present in history; branch had already advanced to `0242d53f2f8fb343782d9387a9b8ea4f0e0d81be` with origin in sync before this block's uncommitted changes.
- Origin divergence at start: `0 ahead / 0 behind`
- Final HEAD: `f6c723dcb22dbbe2bf2c88f14a6f287541400a36`
- Commit: `f6c723dcb22dbbe2bf2c88f14a6f287541400a36`
- Push status: `PUSHED_OK`
- Origin sync: `0 ahead / 0 behind`
- Protected scope diff: `PASS`, no protected path diff detected.

## Modules Found

- `REUSE_AS_IS`: WCU foundation, WCU OCR/Robust Perception interop, disabled UIA read-only collector, Core UIA safety policies.
- `WRAP_ONLY`: Robust Perception and OCR evidence/observation modules.
- `EXTEND_LATER`: `OneBrain.Observation` read-only namespaces as future adapter reference.
- `LEGACY_REFERENCE_ONLY`: `OneBrain.Actions/Uia`, `OneBrain.Pilot` FlaUI harness, `OneBrain.Verification` UIA action verifier.
- `DO_NOT_TOUCH`: CBPR/browser live line and Stealth Core protected scope.

Details: `docs/qa/computer-use/wcu-015-022-win32-uia-events/inventory.md`.

## Files Created/Modified

Created:

- `src/OneBrain.WindowsComputerUse/Win32ReadOnlyContext.cs`
- `src/OneBrain.WindowsComputerUse/WindowsUiAutomationEventStream.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUseReadOnlyContextEvidence.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseWin32UiaEventsTests.cs`
- `docs/qa/computer-use/wcu-015-022-win32-uia-events/inventory.md`
- `docs/architecture/computer-use/windows-computer-use-win32-context-v1.md`
- `docs/architecture/computer-use/windows-computer-use-uia-events-v1.md`
- `docs/qa/computer-use/wcu-015-022-win32-uia-events/report.md`
- `docs/qa/computer-use/wcu-015-022-win32-uia-events/report.json`
- `docs/handoff/nodal-os-wcu-win32-context-uia-events-read-only-fixture-handoff.md`
- `docs/prompts/computer-use/next-wcu-locator-fusion-evidence-redaction-prompt.md`

Modified:

- `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs`

## Implementation Summary

- Added passive Win32 context DTOs and collectors:
  - `IWin32ContextReadOnlyCollector`
  - `FixtureWin32ContextReadOnlyCollector`
  - `DisabledWin32ContextReadOnlyCollector`
- Added passive UIA event DTOs and streams:
  - `IWindowsUiAutomationEventStream`
  - `FixtureWindowsUiAutomationEventStream`
  - `DisabledWindowsUiAutomationEventStream`
- Extended WCU fusion request/result with optional Win32 context and UIA event state.
- Added evidence packs for Win32 context and UIA event observations.
- Extended redaction to cover Windows user profile paths.
- Added tests for active window, modal, DPI, stale/unresponsive, sensitive event payload, and no-authority behavior.

## Validation

| Validation | Result |
| --- | --- |
| `dotnet restore .\OneBrain.slnx` | `PASS` |
| `dotnet build .\OneBrain.slnx --no-restore` | `PASS` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseFixtureSafe` | `PASS 42/42` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseOcrInterop` | `PASS 16/16` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseWin32UiaEvents` | `PASS 14/14` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=CloakBrowserPerceptionRouter` | `PASS 83/83` |
| `git diff --check` | `PASS` |
| `git diff --cached --check` | `PASS` |
| JSON validation | `PASS` |
| Protected scope scan | `PASS` |
| No-live usage scan | `PASS` |
| Bad UX wording scan | `PASS_NEGATIVE_ONLY` |
| Secret scan changed/new | `PASS_FIXTURE_ONLY` |

## No-Live Proof

- No P/Invoke added.
- No FlaUI package or live adapter added.
- No `SetForegroundWindow`, `SendInput`, `PostMessage`, `SendMessage`, keyboard, mouse, clipboard, screenshot, browser live, CDP live, WebSocket, or Safe Injection path added.
- Disabled collectors return disabled states and all action flags false.
- Fixture collectors and streams set `ReadRealPc=false`, `LiveSubscribed=false`, `ActionAuthority=false`, and `EventTriggeredExecution=false`.
- Secret-pattern matches are limited to deliberately fake fixture/test values used to assert evidence redaction.

## Updated Percentages

- WCU fixture-safe foundation: `100%`
- WCU OCR/Robust Perception interop design: `72%`
- WCU Win32 context design readiness: `65%`
- WCU UIA events design readiness: `60%`
- WCU UIA read-only design readiness: `45%`
- WCU UIA live read-only readiness: `12%`
- WCU controlled action readiness: `0%`
- WCU product automation readiness: `0%`
- OCR authority over actions: `0%`
- UIA events authority over actions: `0%`
- Win32 authority over actions: `0%`

## Remaining Risks

- Win32 context is fixture-only; no live read-only collector was run or claimed.
- UIA event stream is fixture-only; no live subscription was run or claimed.
- Real read-only adapter design still needs isolation proof before any live collector is enabled.
- Locator fusion still needs a deeper evidence/redaction pass before any controlled action planning changes.

## Next Recommended Block

`WCU-023-030 -- Locator Fusion + Evidence Redaction Hardening`

Focus: strengthen locator fusion across UIA, Win32, UIA events, and visual signals, with no execution authority and stronger evidence redaction invariants.
