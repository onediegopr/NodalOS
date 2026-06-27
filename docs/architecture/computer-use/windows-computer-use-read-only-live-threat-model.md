# Windows Computer Use Read-Only Live Threat Model

## Scope

This threat model prepares a future gated read-only Windows/UIA/Win32 prototype. It does not approve live collection, product automation, input injection, screenshots, clipboard access, or execution.

Canonical rule: perception is not authorization. Read-only is not action authority.

## Threats

| Area | Risk | Required mitigation |
| --- | --- | --- |
| UIA live read-only | UIA metadata can expose private labels, document names, account names, or credential field structure. | Redact before evidence, avoid credential value capture, scope to allowlisted test apps only. |
| Win32 live read-only | Window titles and process paths can contain customer, project, username, or account data. | Treat titles and paths as sensitive; redact usernames and detected secrets. |
| UIA event subscription | Event streams can flood logs, create false confidence, or be abused as execution triggers. | Disabled by default, throttled, debounced, no action callbacks, audit required. |
| OCR/screenshot leakage | Screenshots can include secrets, logged-in sessions, customer data, or regulated data. | No raw screenshots by default; OCR only through existing redacted bridge; no screenshot persistence. |
| Sensitive surface | Login, OTP, payment, password manager, and admin prompts may be visible. | Block/handoff immediately; no credential value capture; no automation. |
| Stale elements | Cached UIA identities may point to a different element after modal/window changes. | Stale risk and event continuity reduce confidence and require handoff. |
| Modal/UAC/admin | Admin prompts or modal dialogs can block the desktop and create high-risk affordances. | UAC/admin automation is prohibited; modal blockers require handoff. |
| Accidental action boundary | Read-only code can drift into Invoke, SetValue, focus, window messages, or input APIs. | Static scan, explicit gates, disabled collectors, no action authority flags. |
| Test environment | Tests might accidentally read the operator PC or interact with real windows. | Fixture-only tests; disabled collectors return metadata-only disabled status. |
| Audit log tampering | Evidence could be edited or generated without trace. | Tamper-guard hash and audit log required before future prototype. |
| False confidence | High locator/UIA/Win32 confidence can be mistaken for permission. | Confidence is evidence only; policy and future approval remain separate. |
| Privacy | User/customer data can appear in labels, titles, paths, event payloads, OCR, or evidence. | Redaction first, no raw screenshots, no clipboard, no logged-in browser/email/payment surfaces. |

## Explicit Non-Goals

- No Windows actions.
- No mouse or keyboard input.
- No UIA/FlaUI Invoke, Click, or SetValue.
- No window manipulation or focus stealing.
- No clipboard access.
- No raw screenshot persistence.
- No credential value capture.
- No UAC/admin automation.
- No product automation or product UI enablement.

## Decision Boundary

External audit reconciliation after this design pack recorded containment PASS but live advancement NO-GO. `WCU-037-044 — READ-ONLY LIVE PROTOTYPE GATED` is `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO`; the next allowed work is containment/redaction/evidence/policy review only.
