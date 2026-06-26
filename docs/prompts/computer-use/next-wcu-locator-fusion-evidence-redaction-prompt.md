# Next Prompt: WCU Locator Fusion + Evidence Redaction Hardening

Continue NODAL OS on branch `chrome-lab-001-extension-local-ai-bridge`.

Block:

`WCU-023-030 -- Locator Fusion + Evidence Redaction Hardening`

Expected previous decision:

`GO_WCU_WIN32_CONTEXT_UIA_EVENTS_READ_ONLY_FIXTURE_READY`

Hard rules:

- Do not run live Windows actions.
- Do not move mouse or send keyboard.
- Do not use UIA/FlaUI Invoke, Click, SetValue, keyboard, mouse, or clipboard.
- Do not subscribe to live UIA events by default.
- Do not read the real PC by default in tests.
- Do not manipulate windows or foreground state.
- Do not add P/Invoke unless isolated, disabled, read-only, and not executed by default.
- Do not capture or persist raw screenshots.
- Do not duplicate OCR.
- Do not touch browser live, CDP live, WebSocket live, Safe Injection live, or Stealth Core protected scope.
- Do not claim production-ready or live-ready.

Allowed focus:

- Improve fixture-only locator fusion across UIA semantic tree, Win32 context, UIA events, and visual/OCR bridge signals.
- Strengthen evidence refs and redaction invariants.
- Add tests under `tests/OneBrain.Safety.Tests/**` with `TestCategory=WindowsComputerUseFixtureSafe`.
- Update docs under `docs/architecture/computer-use/**`, `docs/qa/computer-use/**`, `docs/handoff/**`, and `docs/prompts/computer-use/**`.

Required cases:

- UIA strong locator remains preferred over visual hints.
- Win32 active window mismatch downgrades locator confidence.
- UIA event stale/unresponsive blocks locator promotion.
- OCR-only target remains handoff.
- Sensitive event payload blocks and redacts evidence.
- Modal/overlay evidence blocks locator promotion.
- Low-confidence fused locator cannot authorize actions.
- Evidence pack never contains raw screenshots, clipboard, raw token/API key, JWT, email, card, password, or Windows user profile path.

Required validation:

- `dotnet restore .\OneBrain.slnx`
- `dotnet build .\OneBrain.slnx --no-restore`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseFixtureSafe`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseOcrInterop`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseWin32UiaEvents`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=CloakBrowserPerceptionRouter`
- `git diff --check`
- `git diff --cached --check`
- JSON validation
- protected scope scan
- no-live usage scan
- bad UX wording scan
- secret scan changed/new

Expected decision if all fixture-safe validations pass:

`GO_WCU_LOCATOR_FUSION_EVIDENCE_REDACTION_READY`
