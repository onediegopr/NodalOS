# WCU-015-022 Win32 Context + UIA Events Inventory

Status: audit-only inventory for fixture-safe Win32/UIA event interop.

Branch: `chrome-lab-001-extension-local-ai-bridge`
Expected base HEAD: `9072c3ec34e3fcd5eb9da9e70d13b467deabb880`
Actual resumed HEAD: `0242d53f2f8fb343782d9387a9b8ea4f0e0d81be`

## Guard Summary

- Branch matched expected branch at resumed guard.
- Expected base was present in history; branch had already advanced to `0242d53f2f8fb343782d9387a9b8ea4f0e0d81be` with origin in sync before this block's uncommitted changes.
- Origin divergence at start: `0 ahead / 0 behind`.
- Worktree at start: clean.
- Protected scope diff at start: none.
- Browser live/CDP/live bridge gates: not run.
- Windows live actions: not run.
- Live UIA event subscription: not run.
- OCR engine creation: not performed.

## Inventory

| Area | Module | Evidence path | Classification | WCU use |
| --- | --- | --- | --- | --- |
| WCU fixture-safe foundation | Snapshot, classifier, blockage detector, sensitive surface detector, planner, evidence pack | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `REUSE_AS_IS` | Base snapshot and policy model for this block. |
| WCU OCR/Robust Perception interop | Visual bridge contracts and fusion classifier | `src/OneBrain.WindowsComputerUse/VisualPerceptionInterop.cs`; `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs` | `REUSE_AS_IS` | Extended with optional Win32 context and UIA event inputs. |
| WCU read-only UIA collector skeleton | Disabled read-only collector | `src/OneBrain.WindowsComputerUse/WindowsUiAutomationReadOnlyCollector.cs` | `REUSE_AS_IS` | Continues to prove no live UIA action channel is exposed. |
| UIA action executors | UIA action/type/read/pattern executors | `src/OneBrain.Actions/Uia/**` | `LEGACY_REFERENCE_ONLY` | Historical/live-capable execution surface. Not reused by WCU fixture contracts. |
| Verification executor | Basic verifier using UIA action executor | `src/OneBrain.Verification/Engine/BasicActionVerifier.cs` | `LEGACY_REFERENCE_ONLY` | Contains historical action verification path. Not reused. |
| Pilot UIA harness | Pilot supervised click harness and FlaUI usage | `src/OneBrain.Pilot/Program.cs` | `LEGACY_REFERENCE_ONLY` | Live/local pilot harness. Not reused. |
| Observation UIA/Windows modules | UIA/window observation namespaces | `src/OneBrain.Observation/**` | `EXTEND_LATER` | Could inform future read-only adapter design, but not called in this block. |
| Core UIA contracts | Safe click/type/read contracts and policies | `src/OneBrain.Core/Execution/**`; `tests/OneBrain.Safety.Tests/Safe*Tests.cs` | `REUSE_AS_IS` | Policy precedent for guarded UIA usage. WCU remains dry-run only. |
| Robust perception | Liveness, overlay, empty surface, semantic fallback | `src/OneBrain.BrowserExecutor.Cdp/NodalOsRobustPerceptionServices.cs` | `WRAP_ONLY` | Existing fallback context. No duplication. |
| OCR observation/evidence | Low-risk OCR observation and evidence ledger | `src/OneBrain.BrowserExecutor.Cdp/NodalOsLowRiskOcrObservationServices.cs`; `src/OneBrain.BrowserExecutor.Contracts/NodalOsOcrEvidenceLedgerContracts.cs` | `WRAP_ONLY` | Existing OCR evidence remains separate. WCU consumes fixture signals only. |
| Browser perception router | CBPR fixture-safe browser line | `src/OneBrain.BrowserPerception/**` | `DO_NOT_TOUCH` | Browser line remains closed/design-only. |
| Stealth Core protected scope | Stealth engine protected paths | `stealth-engine/src/**` protected subset | `DO_NOT_TOUCH` | No diff allowed. |

## Live Historical Modules

The following are explicitly not reused for WCU fixture-safe Win32/UIA event contracts:

- `src/OneBrain.Actions/Uia/UiaActionExecutor.cs`
- `src/OneBrain.Actions/Uia/UiaPatternExecutor.cs`
- `src/OneBrain.Actions/Uia/UiaTypeExecutor.cs`
- `src/OneBrain.Pilot/Program.cs` FlaUI/Windows harness paths
- `src/OneBrain.Verification/Engine/BasicActionVerifier.cs`

Classification: `LEGACY_REFERENCE_ONLY`.

## New Fixture-Safe Surface

This block adds only passive, fixture-safe WCU contracts:

- `Win32WindowContext`
- `IWin32ContextReadOnlyCollector`
- `FixtureWin32ContextReadOnlyCollector`
- `DisabledWin32ContextReadOnlyCollector`
- `WindowsUiAutomationEvent`
- `IWindowsUiAutomationEventStream`
- `FixtureWindowsUiAutomationEventStream`
- `DisabledWindowsUiAutomationEventStream`
- `ComputerUseReadOnlyContextEvidencePack`

No P/Invoke, FlaUI dependency, live subscription, real PC read, input injection, window manipulation, clipboard, or screenshots were added.
