# WCU-008-014 UIA Read-Only + OCR/Robust Perception Interop Audit

Decision: `GO_WCU_UIA_READ_ONLY_OCR_INTEROP_READY`

## Scope

This block added fixture-safe WCU interop contracts and policy tests. It did not create a new OCR engine, did not invoke live OCR providers, did not run browser/CDP/live bridge gates, and did not perform Windows actions.

## Git Baseline

- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial expected HEAD: `1aadf7543afa25c50816bf05b93cf4f9d8c53d8c`
- Initial actual HEAD in target worktree: `1aadf7543afa25c50816bf05b93cf4f9d8c53d8c`
- Origin divergence at start: `0 ahead / 0 behind`
- Implementation commit: `aabe109d7f021b401564f57bd81cef5af7e10308`
- Hardening commit: `WCU-008-014A_OCR_INTEROP_AUDIT_HARDENING`
- Final HEAD: `9072c3ec34e3fcd5eb9da9e70d13b467deabb880`
- Push status: `PUSHED_OK`
- Origin sync: `0 ahead / 0 behind`
- Protected scope diff: `PASS`, no protected path diff detected.

Preexisting dirty worktree before this block:

- `OneBrain.slnx`
- `tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj`
- `src/OneBrain.DocumentIntelligence/**`
- `tests/OneBrain.Safety.Tests/MistralOcrProviderRouterDesignOnlyTests.cs`

## OCR/Perception Modules Found

- `REUSE_AS_IS`: ONNX OCR pipeline, low-risk OCR observation evaluator, OCR observation isolation, pixel/redaction preconditions, WCU evidence redactor, robust perception tests.
- `WRAP_ONLY`: Robust Perception liveness/overlay/empty/semantic fallback services, OCR/Vision provider contracts, OCR evidence ledger, Mistral OCR Provider Router.
- `EXTEND_LATER`: OCR-assisted verification as future read-only verification aid.
- `DO_NOT_TOUCH`: CBPR/browser runtime line and protected Stealth Core.
- `LEGACY_REFERENCE_ONLY`: PaddleOCR Python worker and historical diagnostic path.

Details: `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/inventory.md`.

## Files Created/Modified

Created:

- `src/OneBrain.WindowsComputerUse/VisualPerceptionInterop.cs`
- `src/OneBrain.WindowsComputerUse/WindowsUiAutomationReadOnlyCollector.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseOcrInteropTests.cs`
- `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/inventory.md`
- `docs/architecture/computer-use/windows-computer-use-robust-perception-interop-v1.md`
- `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/report.md`
- `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/report.json`
- `docs/handoff/nodal-os-wcu-uia-read-only-ocr-interop-handoff.md`
- `docs/prompts/computer-use/next-wcu-win32-context-uia-events-read-only-prompt.md`

Preexisting modified/untracked files were not authored by this block and were not intentionally changed.

## Implementation Summary

- Added passive WCU visual contracts for redacted text/element observations and provider-neutral bridge results.
- Added disabled UIA read-only collector skeleton with explicit prohibitions for Invoke, Click, SetValue, keyboard, mouse, clipboard, and screenshots.
- Added fixture visual bridge that wraps existing OCR/Robust Perception-style observations without invoking providers.
- Added perception fusion classifier/planner that combines UIA fixture metadata and visual fixture signals.
- Added adversarial tests proving OCR/visual observations do not authorize actions.

## Validation

| Validation | Result |
| --- | --- |
| `dotnet restore .\OneBrain.slnx` | `PASS` |
| `dotnet build .\OneBrain.slnx --no-restore` | `PASS` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseFixtureSafe` | `PASS 24/24` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseOcrInterop` | `PASS 12/12` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=CloakBrowserPerceptionRouter` | `PASS 83/83` |
| `git diff --check` | `PASS` |
| `git diff --cached --check` | `PASS` |
| JSON validation | `PASS` |
| Protected scope scan | `PASS` |
| No-live usage scan | `PASS`, no live API patterns found in WCU interop code/tests |
| Bad UX wording scan | `PASS_NEGATIVE_ONLY`, only no-claim wording found |
| Secret scan changed/new | `PASS_FIXTURE_ONLY`, fake adversarial test values only and covered by redaction assertions |

## No-Live Proof

- No FlaUI package added.
- No live UIA adapter added.
- No calls to mouse, keyboard, clipboard, screenshot capture, browser live, CDP live, WebSocket live, network OCR, or shell/subprocess execution were added.
- Tests use fixture bridge inputs only.
- `WindowsUiAutomationReadOnlyCollectorDisabled` returns `SkippedDisabled` and action-channel flags remain false.

## Readiness Percentages

- WCU fixture-safe foundation: `100%`
- WCU OCR/Robust Perception interop design: `70%`
- WCU UIA read-only design readiness: `35%`
- WCU UIA live read-only readiness: `10%`
- WCU controlled action readiness: `0%`
- WCU product automation readiness: `0%`
- OCR authority over actions: `0%`

## Remaining Risks

- WCU does not yet consume real Robust Perception/OCR result types directly; current bridge is provider-neutral and fixture-only.
- No live UIA read-only collector was run or claimed.
- Win32 context and UIA event streams remain design/placeholder inputs for the next block.
- Existing dirty Document Intelligence/Mistral router files were present before this block and should be committed or reconciled separately if they are intended branch state.

## Next Recommended Block

`WCU-008-015 -- Win32 Context + UIA Events Read-Only Fixture Interop`

Focus: add passive Win32 context contracts and UIA event fixture stream, map them into the same fusion classifier, and keep all live adapters disabled until a separate read-only proof block.
