# Audit Prompt: WCU Read-Only Live Architecture Safety

Review the NODAL OS `WCU-031-036 — READ-ONLY LIVE DESIGN GATE + AUDIT PACK` artifacts.

Validate:

- No action authority is granted by OCR, UIA events, Win32 context, locator confidence, evidence, or read-only observation.
- No hidden live implementation is introduced.
- No product automation or product UI enablement is claimed.
- `WCU_LIVE_READ_DISABLED_BY_DEFAULT` and every required gate are represented.
- Read-only live is scoped to a future prototype only, disabled by default, and limited to allowlisted test apps.
- Sensitive surfaces, UAC/admin prompts, credential dialogs, payment screens, browsers with logged-in sessions, email/DM apps, password managers, and customer data windows are disallowed.
- Evidence is redacted metadata only.
- Raw screenshot persistence and clipboard evidence are prohibited.
- UIA events cannot trigger actions.
- OCR/Vision remains a fallback observation path and does not authorize actions.
- Protected stealth/browser scope remains untouched.

Return:

- `GO` or `NO_GO`.
- Blocking findings with file references.
- Missing gates or unclear claims.
- Whether containment remains intact. Do not treat containment PASS as authorization to run a live prototype.
