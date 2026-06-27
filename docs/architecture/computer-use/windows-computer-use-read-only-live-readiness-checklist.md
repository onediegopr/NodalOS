# Windows Computer Use Read-Only Live Readiness Checklist

External audit reconciliation status: `AUDIT_CONTAINMENT_PASS_BUT_LIVE_ADVANCE_NO_GO`.

`WCU-037-044 — READ-ONLY LIVE PROTOTYPE GATED` is `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO`. This checklist is retained as a safety boundary, not as an implementation authorization.

## Future Prototype Preconditions

| Criterion | Required state |
| --- | --- |
| Operating system | Windows only. |
| Default state | Disabled by default; no real PC read unless explicitly gated. |
| Scope | Allowlisted test apps only. |
| Evidence | Redacted metadata only. |
| Screenshots | No raw screenshot persistence. |
| Clipboard | No reads or writes. |
| Actions | No Invoke, Click, SetValue, keyboard, mouse, window messages, or focus manipulation. |
| Events | Throttled/debounced and unable to trigger actions. |
| Operator | Explicit human confirmation before any future prototype run. |
| Audit | External architecture and technical no-action audits must pass first. |
| Rollback | Global and per-provider kill switches documented and tested. |

## Allowlisted Test Apps

- Notepad.
- Calculator.
- Explorer limited to a disposable test folder.
- Settings in read-only observation mode.

## Disallowed Surfaces

- Credential dialogs.
- Browsers with logged-in sessions.
- Payment screens.
- Email and direct-message apps.
- Password managers.
- Admin/UAC prompts.
- Banking, fiscal, government, or regulated apps.
- Customer data windows.

## Required Evidence

- Redacted UIA metadata.
- Redacted Win32 metadata.
- Redacted UIA event metadata.
- Redacted OCR/visual observations through the existing bridge only.
- Gate evaluation result.
- Kill-switch state.
- No action authority flag.
- No raw screenshot flag.
- No clipboard flag.
- Audit/tamper guard metadata.

## Required Operator Steps

1. Confirm external audit passed.
2. Confirm test machine and app allowlist.
3. Confirm no logged-in sensitive apps are in scope.
4. Confirm kill switch is available and tested.
5. Confirm live-read prototype remains disabled until explicit run approval.
6. Confirm collection stops immediately on sensitive surface, UAC/admin, or unexpected app.

## Required Rollback Steps

1. Trigger global disable.
2. Disable UIA provider.
3. Disable Win32 provider.
4. Disable UIA event stream.
5. Disable visual/OCR bridge.
6. Stop evidence capture except minimal refusal/audit metadata.
7. Preserve redacted audit record of the rollback decision.

## Required Proof Before Implementation

- Static scan shows no input APIs.
- Static scan shows no window manipulation APIs.
- Static scan shows no clipboard APIs.
- Static scan shows no raw screenshot persistence.
- Tests prove OCR, UIA events, Win32 context, locator confidence, and evidence cannot grant action authority.
- Protected scope remains unchanged.
