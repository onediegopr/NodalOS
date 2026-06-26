# WCU-031-036 Read-Only Live Design Gate Inventory

## Guard

- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Expected base HEAD: `990eef67686782de6d578a614b44ca75107f5b94`
- Actual starting HEAD: `bb0aff351325427fa436793b4f82e01784ef25db`
- Origin divergence at guard: `0 0`
- Worktree at guard: clean except current WCU-031-036 work after implementation started.
- Protected scope diff at guard: none.
- Note: expected base is an ancestor of actual starting HEAD. Two later documentation metadata commits were already present and were not reverted.

## Readiness Classification

| Component | Current module | Classification | Notes |
| --- | --- | --- | --- |
| WCU foundation | `WindowsComputerUseControlPlane.cs` | `READY_FOR_AUDIT` | Fixture-safe foundation remains metadata-only and action-denied. |
| OCR/Robust Perception interop | `VisualPerceptionInterop.cs` | `READY_FOR_AUDIT` | Existing OCR/vision outputs are wrapped as redacted observations. No OCR engine is duplicated. |
| Win32 context | `Win32ReadOnlyContext.cs` | `READY_FOR_DESIGN_REVIEW` | Fixture and disabled collectors only. No P/Invoke or real PC read by default. |
| UIA read-only snapshot | `WindowsUiAutomationReadOnlyCollector.cs` | `READY_FOR_DESIGN_REVIEW` | Disabled collector returns `SkippedDisabled`; action channels stay false. |
| UIA events | `WindowsUiAutomationEventStream.cs` | `READY_FOR_DESIGN_REVIEW` | Fixture and disabled streams only. Events cannot trigger execution. |
| Perception fusion | `ComputerUsePerceptionFusion.cs` | `READY_FOR_AUDIT` | Combines UIA, Win32, events, and visual signals without action authority. |
| Locator fusion | `ComputerUseLocatorFusion.cs` | `READY_FOR_AUDIT` | Confidence and ambiguity are evidence, not authorization. |
| Evidence/redaction | `ComputerUseUnifiedEvidence.cs`, `ComputerUseReadOnlyContextEvidence.cs` | `READY_FOR_AUDIT` | Redacted metadata only; raw screenshots and clipboard are false. |
| Live read-only provider | no active provider | `NOT_READY_FOR_LIVE` | This block creates a design gate and audit pack only. |
| Action executor | none in WCU live path | `BLOCKED_BY_POLICY` | Controlled action readiness remains 0%. |
| Product automation UI | none | `BLOCKED_BY_POLICY` | No product UI enablement for real Windows actions. |
| Human approval for future live-read prototype | docs/prompts only | `REQUIRES_HUMAN_DECISION` | External audit and explicit operator confirmation are required before any future prototype. |

## Module Inventory

| Module | Classification | Rationale |
| --- | --- | --- |
| `src/OneBrain.WindowsComputerUse/WindowsComputerUseControlPlane.cs` | `READY_FOR_AUDIT` | Core fixture snapshots, detectors, and dry-run planner deny live execution. |
| `src/OneBrain.WindowsComputerUse/VisualPerceptionInterop.cs` | `READY_FOR_AUDIT` | Passive bridge to existing OCR/Robust Perception signals; no live provider call in fixtures. |
| `src/OneBrain.WindowsComputerUse/Win32ReadOnlyContext.cs` | `READY_FOR_DESIGN_REVIEW` | Defines Win32 metadata contracts plus fixture/disabled collectors. |
| `src/OneBrain.WindowsComputerUse/WindowsUiAutomationReadOnlyCollector.cs` | `READY_FOR_DESIGN_REVIEW` | Disabled UIA collector blocks Invoke/Click/SetValue/keyboard/mouse/clipboard/screenshots. |
| `src/OneBrain.WindowsComputerUse/WindowsUiAutomationEventStream.cs` | `READY_FOR_DESIGN_REVIEW` | Fixture/disabled UIA event stream with redaction and no callbacks. |
| `src/OneBrain.WindowsComputerUse/ComputerUsePerceptionFusion.cs` | `READY_FOR_AUDIT` | Fixture-safe fusion and policy planning only. |
| `src/OneBrain.WindowsComputerUse/ComputerUseLocatorFusion.cs` | `READY_FOR_AUDIT` | Selector confidence, ambiguity, stale risk, and handoff reasons only. |
| `src/OneBrain.WindowsComputerUse/ComputerUseUnifiedEvidence.cs` | `READY_FOR_AUDIT` | Unified evidence refs, tamper guard, redaction status, and no action authority. |
| `src/OneBrain.WindowsComputerUse/ComputerUseReadOnlyContextEvidence.cs` | `READY_FOR_AUDIT` | Redacts Win32/UIA event evidence; blocks screenshots and clipboard evidence. |
| `src/OneBrain.WindowsComputerUse/ComputerUseReadOnlyLiveDesignGates.cs` | `READY_FOR_AUDIT` | New design-gate catalog, disabled kill-switch state, and fail-closed evaluation. |
| Historical live automation outside WCU | `DO_NOT_TOUCH` | Protected browser/stealth scopes remain out of scope. |

## No-Live Finding

No active live read-only implementation is introduced by this block. The only new code is a gate catalog and fixture-safe tests. Future live read-only work remains gated by audit, allowlisted test apps, explicit dev flag, kill switch, redaction, and human operator confirmation.
