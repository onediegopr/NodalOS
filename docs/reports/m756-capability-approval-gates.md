# M756 Capability Approval Gates

Project: NODAL OS

M756 defines capability approval gates for future dry-run eligibility. It does not grant productive execution.

## Status

- Capability approval gates: READY.
- Approval mode: manual approval and policy gate planning only.
- Productive unlock: false for every capability.
- Public release: NO-GO.
- Chrome Web Store: NO-GO.
- Product files modified: false.
- Bridge/CSP modified: false.

## Required Gate Semantics

Each high-risk capability requires a user-facing explanation, affected resource declaration, expected evidence, denial path, rollback or compensation boundary, and audit event before any future dry-run can be considered.

Provider/cloud live calls require a secrets boundary and keep live calls disabled. Filesystem write requires a jail and rollback/compensation boundary. Browser automation requires human handoff and explicitly forbids credential, CAPTCHA, or 2FA bypass. Capability unlock requires a capability-specific approval gate.
