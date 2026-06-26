# NODAL OS WCU Win32 Context + UIA Events Read-Only Fixture Handoff

Decision: `GO_WCU_WIN32_CONTEXT_UIA_EVENTS_READ_ONLY_FIXTURE_READY`

## What Changed

- Added fixture-safe Win32 context contracts and collectors.
- Added fixture-safe UIA event stream contracts and disabled stream.
- Extended WCU fusion to consume UIA snapshot, OCR/visual bridge, Win32 context, and UIA events.
- Added evidence packs for Win32 context and UIA events.
- Added redaction for Windows user profile paths, window titles, and event payloads.
- Added `WindowsComputerUseWin32UiaEvents` tests.

## Boundaries Preserved

- No live Windows actions.
- No P/Invoke.
- No FlaUI dependency.
- No live UIA event subscription.
- No real PC read by default in tests.
- No window manipulation, focus stealing, input injection, clipboard, or screenshots.
- No browser live/CDP/WebSocket/Safe Injection.
- No OCR duplication.
- No Stealth Core protected changes.
- Win32 and UIA events authority over actions remains `0%`.

## Important Files

- `src/OneBrain.WindowsComputerUse/Win32ReadOnlyContext.cs`
- `src/OneBrain.WindowsComputerUse/WindowsUiAutomationEventStream.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUseReadOnlyContextEvidence.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseWin32UiaEventsTests.cs`
- `docs/architecture/computer-use/windows-computer-use-win32-context-v1.md`
- `docs/architecture/computer-use/windows-computer-use-uia-events-v1.md`
- `docs/qa/computer-use/wcu-015-022-win32-uia-events/report.md`

## Validation Snapshot

- Restore: PASS.
- Build: PASS.
- WCU fixture-safe: PASS `38/38`.
- WCU OCR interop: PASS `16/16`.
- WCU Win32/UIA events: PASS `14/14`.
- CBPR: PASS `83/83`.

## Carry Forward

Next block should focus on locator fusion and evidence redaction hardening. Keep execution authority at zero. Do not add live Win32/UIA adapters until a separate read-only proof block with explicit disabled-by-default behavior and no action APIs.
