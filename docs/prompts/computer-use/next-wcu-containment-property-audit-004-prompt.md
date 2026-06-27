# Next WCU Containment Prompt

Continue NODAL OS on branch `chrome-lab-001-extension-local-ai-bridge`.

Block:

`WCU-CONTAINMENT-PROPERTY-AUDIT-004 — STATIC SCAN HARNESS + PROTECTED BOUNDARY CONSOLIDATION`

Purpose:

Consolidate fixture-safe static scans for WCU containment, protected scope boundaries, no-live/no-action strings, report JSON parsing, synthetic secret handling, and bad-wording classification. This is containment-only work.

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

- Add or consolidate static scan fixtures for protected scope, no-live/no-action, bad wording, and synthetic secret classification.
- Verify reports and prompts remain containment-only.
- Verify static scans distinguish passive prohibition strings from live implementation.
- Verify synthetic redaction fixtures are not treated as real secrets.
- Keep all scan results reportable without enabling live code.

Expected final decision:

`GO_WCU_CONTAINMENT_PROPERTY_AUDIT_004_STATIC_SCAN_BOUNDARY_READY`
