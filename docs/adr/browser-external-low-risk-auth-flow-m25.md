# ADR: Browser External Low-Risk Auth Flow M25

## Status

Accepted for M25.

## Context

M23/M24 proved authenticated browser execution only against a local sandbox. M25 adds the first external authentication boundary, but only for explicitly low-risk, test-owned targets. This is not approval for AFIP, banking, ERP, fiscal portals, customer accounts, personal accounts, payments, irreversible submits, or sites with sensitive personal data.

## Decision

External authentication is represented by `BrowserExternalAuthTarget`, `BrowserExternalAuthPolicy`, `BrowserExternalAuthAttempt`, and `BrowserExternalAuthResult`.

The target must be classified as all of:

- Low risk.
- Non-financial.
- Non-fiscal.
- Non-ERP.
- No irreversible actions.
- No sensitive personal data.
- Read-only after login.

The evaluator fails closed when:

- The host is not allowlisted.
- The target appears financial, fiscal, ERP, or sensitive.
- CAPTCHA or 2FA is required.
- Consent, policy, gate, profile controlled, or vault test credential requirements are missing.
- The post-login read-only guard is absent.
- Semantic proof is missing.

## Read-Only After Login

`ExternalAuthReadOnlyMode` blocks post-login mutations, submits, uploads, payments, deletes, settings changes, and confirmations. It allows read-only dashboard observation and a separately authorized safe download.

## Success Criteria

External auth does not become `StepDone` from HTTP 200, redirects, form submit, cookies, or `UserCompleted`. Completion requires:

- Executed action.
- Verified state.
- Semantic proof.
- Evidence refs.
- Audit without secrets.

## Credential and Cookie Boundary

Credentials may only come from the sandbox/test vault boundary. Companion never receives secret values. Cookies and session material remain inside the controlled browser/profile and are not written to logs, UI, protocol, evidence, audit, or export.

## M25 External Target

No live external low-risk target is configured in this milestone. The implemented state is a formal guard and policy with `M25BlockedNoSafeExternalTarget` behavior unless a separately approved, test-owned target is configured.

## Out of Scope

- AFIP, banks, ERP, fiscal portals.
- Personal or customer accounts.
- 2FA/CAPTCHA automation.
- Request/response body capture.
- Sensitive header values.
- Irreversible submit or payments.
- Productive replay.

## Consequences

M25 raises the runtime boundary for future external low-risk auth without pretending that sensitive or uncontrolled external authentication is safe. A real external target can only be enabled by allowlist, consent, policy, gate, vault test credential, controlled profile, and read-only proof.
