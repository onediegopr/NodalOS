# Windows Computer Use Protected Boundary Consolidation v1

WCU containment work is isolated from browser live, Stealth Core, and sidepanel hash debt.

## Protected Stealth Core Paths

- `stealth-engine/src/evasion/**`
- `stealth-engine/src/captcha/**`
- `stealth-engine/src/fingerprint/**`
- `stealth-engine/src/behavior/**`
- `stealth-engine/src/proxy/**`
- `stealth-engine/src/antiBlocking/**`
- `stealth-engine/src/handoff/**`
- `stealth-engine/src/StealthSession.js`
- `stealth-engine/src/StealthBrowserManager.js`
- `stealth-engine/src/index.js`
- `stealth-engine/tests/stealth-suite.test.js`

Any diff in these paths is a WCU containment NO-GO.

## WCU Allowed Paths

- `src/OneBrain.WindowsComputerUse/**`
- `tests/OneBrain.Safety.Tests/**` for WCU containment tests only
- `docs/architecture/computer-use/**`
- `docs/qa/computer-use/**`
- `docs/handoff/**`
- `docs/prompts/computer-use/**`

## WCU Forbidden Boundaries

- No desktop live automation.
- No read-only live prototype.
- No real PC reads.
- No P/Invoke, FlaUI, live UIA subscription, mouse, keyboard, window manipulation, clipboard, raw screenshots, provider network live, shell/subprocess runtime, browser live/CDP/WebSocket, or Safe Injection live.
- No product UI enablement.
- No public release or paid beta unlock.

## Separate Lines

- WCU containment work does not authorize browser live.
- WCU containment work does not authorize desktop live.
- WCU containment work does not touch Stealth Core.
- WCU containment work does not resolve `SIDE_PANEL_EXTENSION_HASH_BASELINE_RECONCILIATION`.
- CBPR/browser remains separate from WCU containment.
- Public release and paid beta decisions are not related to WCU containment.

## Blocked Live Prototype

`WCU-037-044` remains `BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO`.
