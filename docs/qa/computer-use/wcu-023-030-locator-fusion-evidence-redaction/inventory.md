# WCU-023-030 Locator Fusion + Evidence/Redaction Inventory

Status: fixture-safe inventory.

Branch: `chrome-lab-001-extension-local-ai-bridge`
Expected base HEAD: `877de6fc30c439febad3eaa0f76d3de719b6b857`

## Guard Summary

- Branch matched expected branch.
- HEAD matched expected base at block start.
- Origin divergence at start: `0 ahead / 0 behind`.
- Worktree at start: clean.
- Protected scope diff at start: none.
- Browser live, CDP live, WebSocket live, Windows live action, UIA live event subscription, OCR provider execution: not run.

## Inventory

| Area | Module | Path | Classification | Notes |
| --- | --- | --- | --- | --- |
| Base WCU snapshot/contracts | `ComputerUseSnapshot`, `UiElementIdentity`, `WindowContext` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `REUSE_AS_IS` | Fixture metadata model reused as primary input. |
| Existing locator engine | `ComputerUseLocatorEngine`, `ComputerUseLocatorCandidate` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `EXTEND_IN_PLACE` | Kept for safe action planner dry-run; canonical fusion layer added beside it. |
| Selector scoring | `ComputerUseLocatorEngine.Score` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `EXTEND_IN_PLACE` | Existing simple scorer remains compatible; fusion adds detailed breakdown. |
| OCR/visual bridge | `VisualPerceptionSignal`, `RedactedVisualObservation`, `RobustPerceptionBridgeResult` | `src/OneBrain.WindowsComputerUse/VisualPerceptionInterop.cs` | `WRAP_ONLY` | Existing OCR/Robust Perception outputs are consumed as redacted hints only. |
| Win32 context | `Win32ContextCollectionResult`, `Win32WindowContext` | `src/OneBrain.WindowsComputerUse/Win32ReadOnlyContext.cs` | `REUSE_AS_IS` | Read-only fixture anchor for active window/process matching. |
| UIA events | `WindowsUiAutomationEventStreamState`, `WindowsUiAutomationEvent` | `src/OneBrain.WindowsComputerUse/WindowsUiAutomationEventStream.cs` | `REUSE_AS_IS` | Read-only event continuity and stale/modal signal. |
| Perception fusion | `ComputerUsePerceptionFusionClassifier` | `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs` | `REUSE_AS_IS` | Existing fusion remains action-authority blocked. |
| Sensitive detector | `ComputerUseSensitiveSurfaceDetector` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `REUSE_AS_IS` | Sensitive state feeds locator fusion handoff. |
| Blockage detector | `ComputerUseBlockageDetector` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `REUSE_AS_IS` | Blockages feed locator fusion stale/handoff. |
| Safe action planner | `ComputerUseSafeActionPlanner` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `REUSE_AS_IS` | Dry-run only; no live execution. |
| Evidence builder | `ComputerUseEvidencePackBuilder` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `EXTEND_IN_PLACE` | Existing pack retained; unified pack added for fusion metadata. |
| Read-only context evidence | `ComputerUseReadOnlyContextEvidenceBuilder` | `src/OneBrain.WindowsComputerUse/ComputerUseReadOnlyContextEvidence.cs` | `REUSE_AS_IS` | Win32/UIA event evidence remains redacted. |
| Redaction | `ComputerUseEvidenceRedactor` | `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `REUSE_AS_IS` | Used consistently for locator/evidence outputs. |
| Canonical locator fusion | `ComputerUseLocatorFusionEngine` and fusion records | `src/OneBrain.WindowsComputerUse/ComputerUseLocatorFusion.cs` | `EXTEND_IN_PLACE` | New fixture-safe fusion layer. |
| Unified evidence | `ComputerUseUnifiedEvidencePackBuilder` | `src/OneBrain.WindowsComputerUse/ComputerUseUnifiedEvidence.cs` | `EXTEND_IN_PLACE` | New evidence/redaction metadata pack. |
| Browser perception / CBPR | Browser fixture-safe line | `src/OneBrain.BrowserPerception/**` | `DO_NOT_TOUCH` | Not modified. |
| Stealth protected scope | Protected Stealth Core paths | `stealth-engine/**` protected subset | `DO_NOT_TOUCH` | No diff allowed. |
| Legacy UIA action surfaces | `OneBrain.Actions/Uia/**`, `OneBrain.Pilot`, `OneBrain.Verification` | Existing non-WCU action paths | `LEGACY_REFERENCE_ONLY` | Not reused for locator fusion. |

## New Fixture-Safe Surface

- `ComputerUseLocatorFusionInput`
- `ComputerUseLocatorFusionResult`
- `ComputerUseSelectorCandidate`
- `ComputerUseSelectorEvidence`
- `ComputerUseSelectorConfidenceBreakdown`
- `ComputerUseLocatorAmbiguity`
- `ComputerUseStaleElementRisk`
- `ComputerUseVisualHintMatch`
- `ComputerUseEventContinuitySignal`
- `ComputerUseWin32AnchorSignal`
- `ComputerUseLocatorHandoffReason`
- `ComputerUseUnifiedEvidencePack`
- `ComputerUseUnifiedEvidencePackBuilder`

No live adapter, P/Invoke, FlaUI dependency, UIA event subscription, screenshots, clipboard, or action execution was added.
