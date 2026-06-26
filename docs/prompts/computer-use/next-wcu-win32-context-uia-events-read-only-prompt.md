# Next Prompt: WCU Win32 Context + UIA Events Read-Only

Continue NODAL OS on branch `chrome-lab-001-extension-local-ai-bridge`.

Block:

`WCU-008-015 -- Win32 Context + UIA Events Read-Only Fixture Interop`

Expected previous decision:

`GO_WCU_UIA_READ_ONLY_OCR_INTEROP_READY`

Hard rules:

- Do not run live Windows actions.
- Do not move mouse or send keyboard.
- Do not use UIA/FlaUI Invoke, Click, SetValue, keyboard, mouse, or clipboard.
- Do not capture or persist raw screenshots.
- Do not add an OCR engine.
- Do not call Mistral/OCR live providers.
- Do not touch browser live, CDP live, WebSocket live, Safe Injection live, or Stealth Core protected scope.
- Do not claim production-ready or live-ready.

Allowed focus:

- Add passive Win32 window context contracts in `src/OneBrain.WindowsComputerUse/**`.
- Add passive UIA event fixture contracts in `src/OneBrain.WindowsComputerUse/**`.
- Extend `ComputerUsePerceptionFusionClassifier` with Win32/UIA event fixture inputs.
- Add tests under `tests/OneBrain.Safety.Tests/**` with `TestCategory=WindowsComputerUseFixtureSafe`.
- Update docs under `docs/architecture/computer-use/**`, `docs/qa/computer-use/**`, `docs/handoff/**`, and `docs/prompts/computer-use/**`.

Required cases:

- Foreground allowlisted window context improves read-only confidence but does not authorize actions.
- Wrong process/title/class lowers confidence and requires handoff.
- Modal foreground owner blocks action planning.
- DPI/monitor mismatch is warning/handoff.
- UIA focus changed event is metadata only.
- UIA tree changed event is metadata only.
- Stale event sequence requires re-snapshot/handoff.
- Win32/UIA event signals cannot override OCR/visual sensitive-surface blockers.

Required validation:

- `dotnet restore .\OneBrain.slnx`
- `dotnet build .\OneBrain.slnx --no-restore`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseFixtureSafe`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=CloakBrowserPerceptionRouter`
- `git diff --check`
- `git diff --cached --check`
- JSON validation for new reports
- protected scope scan
- no-live usage scan
- secret scan changed/new

Expected decision if all fixture-safe validations pass:

`GO_WCU_WIN32_CONTEXT_UIA_EVENTS_READ_ONLY_READY`
