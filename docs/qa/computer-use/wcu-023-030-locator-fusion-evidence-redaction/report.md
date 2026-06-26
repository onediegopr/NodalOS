# WCU-023-030 Locator Fusion + Evidence/Redaction Unification

Decision: `GO_WCU_LOCATOR_FUSION_EVIDENCE_REDACTION_FIXTURE_READY`

## Scope

This block added fixture-safe locator fusion and evidence redaction hardening for Windows Computer Use. It combines UIA snapshot candidates, OCR/visual hints, Win32 read-only context, and UIA event stream metadata into ranked locator candidates with no action authority. It did not add P/Invoke, FlaUI, live UIA subscription, real PC reads, input injection, window manipulation, clipboard access, screenshot capture, browser live, CDP live, WebSocket live, or OCR provider execution.

## Git Baseline

- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `877de6fc30c439febad3eaa0f76d3de719b6b857`
- Origin divergence at start: `0 ahead / 0 behind`
- Final HEAD: recorded after commit
- Commit: recorded after commit
- Push status: recorded after push
- Protected scope diff: `PASS`, no protected path diff detected.

## Modules Found

- `REUSE_AS_IS`: WCU foundation, WCU OCR/Robust Perception interop, WCU Win32/UIA events read-only contracts, disabled UIA read-only collector.
- `WRAP_ONLY`: Robust Perception and OCR evidence/observation modules.
- `EXTEND_LATER`: `OneBrain.Observation` read-only namespaces as future adapter reference.
- `LEGACY_REFERENCE_ONLY`: `OneBrain.Actions/Uia`, `OneBrain.Pilot` FlaUI harness, `OneBrain.Verification` UIA action verifier.
- `DO_NOT_TOUCH`: CBPR/browser live line and Stealth Core protected scope.

## Files Created/Modified

Created:

- `src/OneBrain.WindowsComputerUse/ComputerUseLocatorFusion.cs`
- `src/OneBrain.WindowsComputerUse/ComputerUseUnifiedEvidence.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseLocatorFusionTests.cs`
- `tests/OneBrain.Safety.Tests/WindowsComputerUseLocatorFusionEvidenceTests.cs`
- `docs/architecture/computer-use/windows-computer-use-locator-fusion-v1.md`
- `docs/architecture/computer-use/windows-computer-use-evidence-redaction-unification-v1.md`
- `docs/qa/computer-use/wcu-023-030-locator-fusion-evidence-redaction/inventory.md`
- `docs/qa/computer-use/wcu-023-030-locator-fusion-evidence-redaction/report.md`
- `docs/qa/computer-use/wcu-023-030-locator-fusion-evidence-redaction/report.json`
- `docs/handoff/nodal-os-wcu-locator-fusion-evidence-redaction-fixture-handoff.md`
- `docs/prompts/computer-use/next-wcu-read-only-live-design-gate-audit-prompt.md`

Modified:

- `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` (new evidence kinds, phone/fiscal redaction regexes)

## Implementation Summary

- Added `ComputerUseLocatorFusionEngine` producing ranked `ComputerUseSelectorCandidate` results.
- Added ambiguity detection, stale element risk, Win32 anchor signal, UIA event continuity signal, and visual hint matching.
- Added hostile source detection blocking action authority, live provider calls, raw screenshots, real PC reads, live subscriptions, action callbacks, and event-triggered execution.
- Added `ComputerUseUnifiedEvidencePackBuilder` with SHA-256 tamper guard, audit log bypass guard, and redaction summary.
- Extended `ComputerUseEvidenceRedactor` with phone number and fiscal/bank-like identifier patterns.
- Added 30 fixture-safe tests covering high-confidence candidates, OCR-only/visual-only paths, hostile source flags, redaction of sensitive text, Windows profile paths, phone numbers, fiscal/bank values, ambiguity, stale risk, UAC, and unified evidence packs.

## Validation

| Validation | Result |
| --- | --- |
| `dotnet build .\OneBrain.slnx --no-restore` | `PASS` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseLocatorFusion` | `PASS 30/30` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseLocatorFusionEvidence` | `PASS 15/15` |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter TestCategory=WindowsComputerUseFixtureSafe` | `PASS 87/87` |
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

- No P/Invoke, `DllImport`, `extern`, or `unsafe` added.
- No FlaUI package or live adapter added.
- No `SetForegroundWindow`, `SendInput`, `PostMessage`, `SendMessage`, keyboard, mouse, clipboard, screenshot, browser live, CDP live, WebSocket, or Safe Injection path added.
- `ActionAuthorityGranted`, `AllowedToExecuteLive`, `LiveProviderCalled`, `RealPcRead`, `RawScreenshotStored` are hardcoded false on every `ComputerUseLocatorFusionResult`.
- Fixture streams and collectors continue to set all action/live flags false.
- Unified evidence emits `ActionAuthorityGranted=false`, `RawScreenshotPresent=false`, and `ClipboardPresent=false`.
- Secret-pattern matches are limited to deliberately fake fixture/test values used to assert redaction.

## Updated Percentages

- WCU fixture-safe foundation: `100%`
- WCU OCR/Robust Perception interop design: `78%`
- WCU Win32 context design readiness: `73%`
- WCU UIA events design readiness: `70%`
- WCU locator fusion readiness: `80%`
- WCU evidence/redaction unification: `82%`
- WCU UIA read-only design readiness: `55%`
- WCU UIA live read-only readiness: `15%`
- WCU controlled action readiness: `0%`
- WCU product automation readiness: `0%`
- OCR authority over actions: `0%`
- UIA events authority over actions: `0%`
- Win32 authority over actions: `0%`
- Locator authority over actions: `0%`

## Remaining Risks

- Locator fusion is fixture-only; no live desktop integration was run or claimed.
- Real read-only adapter isolation remains future work.
- Confidence thresholds are deterministic heuristics; live surfaces may need calibration.
- Controlled action planning remains blocked and must be hardened separately.

## Next Recommended Block

`WCU-031-036 -- READ-ONLY LIVE DESIGN GATE + AUDIT PACK`

Focus: design and audit gates for any future UIA/Win32 read-only live adapter, with no action execution.
