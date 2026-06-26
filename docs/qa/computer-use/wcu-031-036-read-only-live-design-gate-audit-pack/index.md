# WCU-031-036 Audit Pack Index

## Decision Target

`GO_WCU_READ_ONLY_LIVE_DESIGN_GATE_AUDIT_PACK_READY`

This pack prepares a future audit decision for a read-only live prototype. It does not implement live read-only collection and does not approve product automation.

## Architecture Documents

- `docs/architecture/computer-use/windows-computer-use-read-only-live-threat-model.md`
- `docs/architecture/computer-use/windows-computer-use-read-only-live-gates.md`
- `docs/architecture/computer-use/windows-computer-use-read-only-live-readiness-checklist.md`

## Audit Prompts

- `docs/prompts/computer-use/audit-wcu-read-only-live-architecture-safety.md`
- `docs/prompts/computer-use/audit-wcu-read-only-live-technical-no-action-verification.md`

## QA Artifacts

- `docs/qa/computer-use/wcu-031-036-read-only-live-design-gate-audit-pack/inventory.md`
- `docs/qa/computer-use/wcu-031-036-read-only-live-design-gate-audit-pack/report.md`
- `docs/qa/computer-use/wcu-031-036-read-only-live-design-gate-audit-pack/report.json`

## Handoff And Next Prompt

- `docs/handoff/nodal-os-wcu-read-only-live-design-gate-audit-pack-handoff.md`
- `docs/prompts/computer-use/next-wcu-read-only-live-prototype-gated-prompt.md`

## Audit Questions

- Does any code path grant action authority from OCR, UIA events, Win32 context, locator confidence, or evidence?
- Is any live provider active by default?
- Are raw screenshots, clipboard, credential values, input injection, and window manipulation excluded?
- Are protected stealth/browser scopes untouched?
- Are claims limited to design gate and audit readiness?
