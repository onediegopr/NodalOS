# NODAL OS WCU UIA Read-Only OCR Interop Handoff

Decision: `GO_WCU_UIA_READ_ONLY_OCR_INTEROP_READY`

## What Changed

- WCU now has passive visual perception DTOs and bridge contracts for existing OCR/Robust Perception outputs.
- WCU has a disabled UIA read-only collector skeleton with all action channels explicitly blocked.
- WCU has a fixture-only fusion classifier/planner that combines UIA fixture metadata and redacted visual signals.
- Safety tests prove OCR/visual observations cannot authorize actions.

## Boundaries Preserved

- No new OCR engine.
- No Mistral/OCR live provider call.
- No raw screenshot persistence.
- No mouse, keyboard, clipboard, Invoke, Click, SetValue, shell, browser live, CDP live, or WebSocket live use.
- No Stealth Core protected files changed.
- No production-ready or live-ready claim.

## Important Files

- `src/OneBrain.WindowsComputerUse/VisualPerceptionInterop.cs`
- `src/OneBrain.WindowsComputerUse/WindowsUiAutomationReadOnlyCollector.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseOcrInteropTests.cs`
- `docs/architecture/computer-use/windows-computer-use-robust-perception-interop-v1.md`
- `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/inventory.md`
- `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/report.md`
- `docs/qa/computer-use/wcu-008-014-uia-ocr-interop/report.json`

## Validation Snapshot

- Restore: PASS.
- Build: PASS.
- WCU fixture-safe tests: PASS `24/24`.
- WCU OCR interop tests: PASS `12/12`.
- CBPR tests: PASS `83/83`.

## Carry Forward

Next block should add passive Win32 window context and UIA event fixture streams, then map those into `ComputerUsePerceptionFusionClassifier`.

Do not add FlaUI live collection until a separate read-only proof block. If a future FlaUI adapter is attempted, it must be behind `IWindowsUiAutomationReadOnlyCollector`, never expose Invoke/Click/SetValue, and must report `NOT_RUN` when the environment cannot prove read-only behavior.
