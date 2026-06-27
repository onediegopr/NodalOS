# Windows Computer Use Active Safe Mode Product Boundary v1

## Purpose

Windows Computer Use is an active product capability only inside its safe boundary:

- `Computer Use Control Plane: READY_SAFE_MODE`
- `SupportedMode: SafeMode / Containment / PerceptionFoundation`
- `LiveDesktopAutomation: DISABLED_BY_POLICY`
- `LiveRead: DISABLED_BY_POLICY`
- `RealMouseKeyboard: DISABLED_BY_POLICY`
- `RawScreenshotCapture: DISABLED_BY_POLICY`
- `ActionAuthority: NONE`
- `FailureBehavior: FAIL_CLOSED`

This resolves hidden/ghost status without authorizing live desktop control. The capability can be shown as a safe control-plane and perception foundation module, not as real PC automation.

## Safe Product Surface

Allowed safe-mode wording:

- "Computer Use Control Plane - Safe Mode Ready"
- "Perception/Evidence/Redaction/Handoff foundation ready"
- "Live desktop control disabled by policy"

Allowed behavior:

- Return structured status from `ComputerUseSafeModeFacade.GetStatus()`.
- Return safe-mode readiness from `GetReadiness()`.
- Return `READY_SAFE_MODE` for safe-mode requests.
- Return `BLOCKED_BY_POLICY` for live read, action execution, browser live/CDP, and product automation requests.
- Preserve `LiveReadPermitted=false`, `ActionAuthorityGranted=false`, and `ProductAutomationEnabled=false`.

## Prohibited Product Claims

These claims are rejected by the safe-mode claim catalog:

- "Controla tu PC real"
- "Live desktop automation ready"
- "Mouse/keyboard automation ready"
- "FlaUI/UIA live ready"
- "Screenshots/live screen capture ready"
- "real PC automation ready"
- "live desktop control ready"

## Fail-Closed Contract

If a caller asks for a mode outside safe mode, WCU returns a structured blocked result:

- `Status=BlockedByPolicy`
- `FailureBehavior=FAIL_CLOSED`
- `IfCalledBehavior=RETURNS_STRUCTURED_SAFE_MODE_RESULT`
- `LiveProviderCalled=false`
- `RawScreenshotPresent=false`
- `ClipboardPresent=false`
- `LiveReadPermitted=false`
- `ActionAuthorityGranted=false`
- `ProductAutomationEnabled=false`

The facade does not read the real PC, subscribe to live UIA events, invoke providers, move mouse, send keys, access clipboard, capture screenshots, start processes, or use browser live/CDP.

## Blocked Live Status

`WCU-037-044` remains `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO`.

`WCU-031-036` remains closed and is not reopened by this product-safe surface. The sidepanel extension hash baseline debt remains separate and untouched.
