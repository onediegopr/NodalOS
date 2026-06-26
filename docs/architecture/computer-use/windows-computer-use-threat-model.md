# Windows Computer Use Threat Model

Date: 2026-06-26

Project: NODAL OS

Scope: fixture-safe/design-first WCU foundation. This threat model does not authorize live automation.

## Protected Assets

- User credentials, OTP, 2FA, tokens, payment data, session material.
- Local files, paths, clipboard content, screenshots, OCR text, and app state.
- UAC/admin decisions and destructive OS operations.
- Audit logs and evidence integrity.
- Product trust: UI must not imply live PC control is enabled.

## Attack Surfaces

- Future UIA/FlaUI read-only provider.
- Future Win32 context collector.
- Future visual/OCR fallback.
- Future action provider.
- Fixture snapshots and evidence serialization.
- Human approval UX.

## Threats And Mitigations

| Threat | Severity | Current Mitigation | Required Before Live |
| --- | --- | --- | --- |
| Credential field action | Critical | Human handoff in planner | Explicit credential policy and audit |
| UAC/admin automation | Critical | UAC blocker routes handoff | Permanent block unless new governance |
| Destructive action | Critical | Destructive metadata blocks planning | Approval model, rollback, audit |
| Clipboard leakage | High | Clipboard capture flag blocks evidence | No capture or redacted explicit policy |
| Screenshot leakage | High | Screenshot persistence prohibited | Visual redaction and storage policy |
| OCR leakage | High | OCR text persistence prohibited | OCR redaction proof |
| App outside allowlist | High | Non-allowlisted app handoff | Allowlist and target proof |
| Low-confidence selectors | High | Handoff on low confidence | Selector validation and audit |
| Visual-only clicking | High | No execution; future fallback note | Separate visual action ADR |
| Remote desktop/Citrix | High | Handoff/blocker | Separate risk model |
| Hidden windows/modals | High | Modal blocker handoff | Modal policy and UX |
| DPI/monitor mismatch | Medium | Handoff/warning | Live calibration tests |
| Evidence tampering | High | Evidence metadata and refs | Tamper-evident ledger |
| Audit log bypass | High | Required validation reporting | Mandatory audit log |

## Trust Boundaries

- Fixture snapshots are test data, not live app authority.
- UIA/Win32/OCR future providers are untrusted until separately audited.
- Page/browser CBPR gates do not authorize Windows automation.
- Human approval must be external to app content and fixture text.

## Decision

`WCU_THREAT_MODEL_READY_FOR_FIXTURE_SAFE_FOUNDATION`

Live Windows automation remains `NO-GO`.
