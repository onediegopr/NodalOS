# Recipe Sensitive Category Gates

Phase: 5/9 - Tool Trust Registry + Secrets by Reference.

Phase 5 hardens the sensitive category policy carried forward from audit finding `F-003`.

The following categories require approval or human intervention unless separately blocked:

- login,
- credential use,
- data deletion,
- data mutation,
- file write,
- external system mutation,
- marketplace listing change,
- price or stock change,
- personal data handling,
- secret handling.

The following categories remain blocked or human-required:

- CAPTCHA/challenge,
- 2FA,
- browser live action,
- desktop live action,
- unknown sensitive action.

Personal data and secret handling also require redaction evidence expectations. Secret values remain by reference only.

Approval does not unlock live browser, live desktop, connector execution, payment/fiscal/message/delete/publication execution, or challenge bypass.
