# Windows Computer Use Read-Only Live Gates

## Purpose

These gates define the minimum design boundary for read-only review. External audit reconciliation recorded containment PASS but live advancement NO-GO, so `WCU-037-044 — READ-ONLY LIVE PROTOTYPE GATED` is blocked pending human policy decision and external GO. The gates do not implement a live provider and do not permit actions.

## Required Gates

| Gate | Requirement | Default |
| --- | --- | --- |
| `WCU_LIVE_READ_DISABLED_BY_DEFAULT` | Live read remains disabled unless a future audit explicitly clears every gate. | Passes only as disabled/fail-closed. |
| `WCU_LIVE_READ_DEV_FLAG_REQUIRED` | A future prototype requires an explicit dev-only flag. | Blocked until opt-in exists and audit approves it. |
| `WCU_NO_INPUT_INJECTION_GATE` | Mouse, keyboard, SendInput, SendKeys, and equivalent input channels are prohibited. | Must pass. |
| `WCU_NO_WINDOW_MANIPULATION_GATE` | Foreground, focus, z-order, PostMessage, SendMessage, and window manipulation are prohibited. | Must pass. |
| `WCU_NO_CLIPBOARD_GATE` | Clipboard read/write is prohibited. | Must pass. |
| `WCU_NO_RAW_SCREENSHOT_GATE` | Raw screenshot capture or persistence is prohibited by default. | Must pass. |
| `WCU_NO_CREDENTIAL_VALUE_CAPTURE_GATE` | Passwords, OTPs, tokens, secrets, and credential values must not be captured. | Must pass. |
| `WCU_NO_UAC_ADMIN_AUTOMATION_GATE` | UAC/admin prompts require human handoff and cannot be automated. | Must pass. |
| `WCU_EVENT_STREAM_NO_ACTION_TRIGGER_GATE` | UIA events are observations and cannot trigger execution. | Must pass. |
| `WCU_EVIDENCE_REDACTION_REQUIRED_GATE` | Evidence must be redacted before reporting or persistence. | Must pass. |
| `WCU_AUDIT_LOG_REQUIRED_GATE` | Any future prototype requires an audit log before collection. | Blocked until audit path exists. |
| `WCU_KILL_SWITCH_REQUIRED_GATE` | Global and per-provider fail-closed kill switches are required. | Required. |
| `WCU_ALLOWLISTED_TEST_APPS_ONLY_GATE` | Future live-read scope is limited to allowlisted test apps. | Blocked until operator selects allowlisted scope. |
| `WCU_HUMAN_OPERATOR_CONFIRMATION_GATE` | A human operator must explicitly confirm any future gated read-only prototype run. | Blocked until confirmed. |

## Kill Switch Design

| Switch | Required behavior |
| --- | --- |
| Global disable | Stops every read-only live provider and leaves the system in fail-closed state. |
| Per-provider disable | Independently disables UIA, Win32, event stream, and visual/OCR providers. |
| Event-stream disable | Prevents live UIA subscriptions and callbacks. |
| Visual/OCR disable | Prevents live visual provider calls and raw screenshot capture. |
| Evidence capture disable | Stops optional evidence capture while still allowing minimal audit refusal logs. |
| Emergency fail-closed | Forces `LiveReadPermitted=false`, `ActionAuthorityGranted=false`, and `ProductAutomationEnabled=false`. |

## Contract Representation

The gate catalog is represented in `ComputerUseReadOnlyLiveDesignGates.cs`. Its evaluation result always denies live read permission and action authority in this block. The external reconciliation record also maps containment PASS to live advancement NO-GO. The contract is an audit artifact, not a live implementation.
