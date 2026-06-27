# Next WCU Containment Prompt

Continue NODAL OS on branch `chrome-lab-001-extension-local-ai-bridge`.

Block:

`WCU-CONTAINMENT-PROPERTY-AUDIT-003 — REPORT/JSON/CLAIM CONSISTENCY + DRIFT LOCK`

Purpose:

Audit and lock consistency across WCU reports, JSON, handoff docs, prompts, code constants, and containment matrices. This is containment-only work.

Required constraints:

- Do not implement desktop live automation.
- Do not implement a read-only live prototype.
- Do not read the real PC.
- Do not add P/Invoke, FlaUI, live UIA subscription, mouse, keyboard, window manipulation, clipboard, raw screenshots, browser live/CDP, WebSocket bridge, or Safe Injection live.
- Do not authorize `WCU-037-044`.
- Keep `WCU-037-044` as `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO`.
- Keep `LiveReadPermitted=false`.
- Keep `ActionAuthorityGranted=false`.
- Keep `ProductAutomationEnabled=false`.
- Do not touch Stealth Core protected scope.
- Do not mix `SIDE_PANEL_EXTENSION_HASH_BASELINE_RECONCILIATION`.

Expected work:

- Verify all WCU report JSON files agree on containment and blocked live status.
- Verify prompts do not recommend live read-only implementation as the next executable block.
- Verify handoff docs do not convert containment pass into live GO.
- Verify code constants and docs use the same blocked status strings.
- Add fixture-safe tests for report/JSON/claim consistency and drift prevention.

Expected final decision:

`GO_WCU_CONTAINMENT_PROPERTY_AUDIT_003_REPORT_JSON_CLAIM_DRIFT_LOCK_READY`
