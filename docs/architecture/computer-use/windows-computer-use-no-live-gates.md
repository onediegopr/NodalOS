# Windows Computer Use No-Live Gates

Date: 2026-06-26

Project: NODAL OS

These gates are mandatory for WCU until a future human decision and audit explicitly change scope.

| Gate | Purpose | PASS | NO-GO |
| --- | --- | --- | --- |
| `NO_REAL_MOUSE_GATE` | Block real pointer movement/clicks | No mouse APIs used | Any real mouse call |
| `NO_REAL_KEYBOARD_GATE` | Block real key input | No keyboard APIs used | Any real keyboard call |
| `NO_LIVE_UIA_ACTION_GATE` | Block live UIA/FlaUI actions | No live UIA provider actions | Invoke/SetValue/click against real app |
| `NO_CLIPBOARD_CAPTURE_GATE` | Block clipboard capture | Clipboard flag false | Clipboard read/write |
| `NO_SCREENSHOT_LEAKAGE_GATE` | Block raw screenshots | Metadata/placeholder only | Raw screenshot persisted |
| `NO_CREDENTIAL_FIELD_ACTION_GATE` | Block credentials | Handoff on sensitive fields | Plan/action on credential |
| `NO_UAC_ADMIN_AUTOMATION_GATE` | Block UAC/admin | Handoff on UAC/admin | Admin prompt automation |
| `NO_DESTRUCTIVE_OS_ACTION_GATE` | Block destructive operations | Handoff/blocked | Delete/overwrite/format action |
| `NO_LOW_CONFIDENCE_VISUAL_CLICK_GATE` | Block visual-only low confidence | Handoff | Visual click planned/executed |
| `NO_AUDIT_LOG_BYPASS_GATE` | Preserve auditability | Validation reported | Unlogged gate transition |
| `NO_EVIDENCE_TAMPERING_GATE` | Preserve evidence integrity | Traceable metadata evidence | Silent evidence mutation |

## Suggested Scans

- Real mouse/keyboard: `rg -n "Mouse|Keyboard|SendKeys|mouse_event|keybd_event|SetCursorPos" src tests`
- Live UIA/FlaUI actions: `rg -n "FlaUI|InvokePattern|SetValue|Click\\(|UIA3Automation" src tests`
- Clipboard: `rg -n "Clipboard|GetText|SetText" src tests`
- Screenshots: `rg -n "screenshot|Bitmap|Capture|Image" src tests`
- Credential/challenge: `rg -n "password|credential|otp|2fa|token|secret|uac|admin" src tests docs`
- Evidence integrity: `rg -n "Evidence|Correlation|Redaction|Audit" src tests docs`

Keyword hits are not automatically failures when they are blocked test literals or governance documentation. Any executable live action path is `NO-GO`.
