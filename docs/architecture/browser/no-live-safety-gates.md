# No-Live Safety Gate Specification

Date: 2026-06-26

Project: NODAL OS

Scope: mandatory gates for every future browser automation block. These gates are governance requirements and do not implement live execution.

## Gate Summary

| Gate | Purpose | PASS Condition | NO-GO Condition |
| --- | --- | --- | --- |
| `NO_LIVE_CDP_GATE` | Block live CDP actions | No live CDP calls or action commands | Any implementation of live action CDP |
| `NO_WEBSOCKET_LIVE_BRIDGE_GATE` | Block live action bridge | No WebSocket live action path | Any live bridge action path |
| `NO_SAFE_INJECTION_LIVE_GATE` | Block live injection | No live injection code | Any injection that can mutate a page |
| `NO_EXTERNAL_NAVIGATION_GATE` | Block external navigation | No external page navigation | Any unapproved external navigation |
| `NO_REAL_ACTION_GATE` | Block real browser actions | Actions remain fixture/in-memory only | Real click/type/select/scroll/wait |
| `NO_PRODUCT_UI_ENABLEMENT_GATE` | Block product action enablement | UI cannot trigger live actions | Product UI enables browser actions |
| `NO_SYSTEM_BROWSER_FALLBACK_GATE` | Block system browser fallback | CloakBrowser pinned runtime only | System browser fallback or channel usage |
| `NO_EXTENSION_DEFAULT_FALLBACK_GATE` | Block extension default fallback | Extension remains legacy/no-default | Extension used as default runtime |
| `NO_STEALTH_CORE_TOUCH_GATE` | Protect Stealth Core | Protected paths unchanged | Any protected path diff |
| `NO_CREDENTIAL_OR_CHALLENGE_BYPASS_GATE` | Block credential/challenge automation | Human handoff for sensitive/challenge cases | Bypass or credential automation |
| `NO_LIVE_COLLECTOR_GATE` | Block live collector implementation | Collector remains design-only or fixture-only | Any live collector reads browser state without new approval |
| `NO_FILESYSTEM_LEAKAGE_GATE` | Block local data leakage | No private paths or file contents leak into evidence | Raw filesystem paths or file contents exposed |
| `NO_PROMPT_INJECTION_WEB_GATE` | Block web content influence over policy | Page content cannot change safety policy | Page text can override gates or approval |
| `NO_SCREENSHOT_LEAKAGE_GATE` | Block raw screenshot leakage | Screenshot metadata only unless separately approved | Raw screenshot persisted or exposed |
| `NO_SESSION_STORAGE_CAPTURE_GATE` | Block session/storage capture | No cookie/storage/session values collected | Cookie, localStorage, sessionStorage, or token values captured |
| `NO_UI_WORDING_OVERCLAIM_GATE` | Block misleading product copy | UI/docs clearly say live is disabled | Copy implies live actions are enabled |
| `NO_EVIDENCE_TAMPERING_GATE` | Protect evidence integrity | Evidence remains traceable and redacted | Evidence can be silently changed or forged |
| `NO_AUDIT_LOG_BYPASS_GATE` | Protect auditability | Required decisions and validations are recorded | Live-relevant steps bypass audit logging |

## `NO_LIVE_CDP_GATE`

- Purpose: prevent live CDP action implementation.
- Blocks: `Runtime.evaluate`, input dispatch, page mutation, action CDP calls, live action collector behavior.
- Paths/keywords to watch: BrowserPerception, runtime adapters, CDP scripts, `Runtime.evaluate`, `Input.dispatch`, `Page.`, `Network.`, `DOM.`, `click`, `type`, `select`.
- Expected evidence: code diff review, grep scan, tests proving fixture-only behavior.
- PASS: no action-capable CDP call is introduced.
- NO-GO: any code can execute live page actions.
- Suggested scan: `rg -n "Runtime\\.evaluate|Input\\.dispatch|Page\\.|Network\\.|DOM\\." src tests scripts`
- Report as: `NO_LIVE_CDP_GATE=PASS/NO_GO`.

## `NO_WEBSOCKET_LIVE_BRIDGE_GATE`

- Purpose: prevent adding a live action bridge.
- Blocks: WebSocket action messages, bridge action routing, live action gateway.
- Paths/keywords to watch: Bridge, WebSocket, `/ws/stealth-core`, `send`, `action`, `execute`.
- Expected evidence: diff review and no bridge/protected scope changes.
- PASS: no new live WebSocket action path exists.
- NO-GO: WebSocket can trigger browser actions.
- Suggested scan: `rg -n "WebSocket|/ws/stealth-core|live action|execute action" src tests docs`
- Report as: `NO_WEBSOCKET_LIVE_BRIDGE_GATE=PASS/NO_GO`.

## `NO_SAFE_INJECTION_LIVE_GATE`

- Purpose: block live Safe Injection.
- Blocks: live script injection, DOM mutation injection, action helper injection.
- Paths/keywords to watch: injection, injected script, execute script, evaluate.
- Expected evidence: no injection code added.
- PASS: no live injection path exists.
- NO-GO: page mutation via injected script is possible.
- Suggested scan: `rg -n "inject|ExecuteScript|evaluate|script" src tests`
- Report as: `NO_SAFE_INJECTION_LIVE_GATE=PASS/NO_GO`.

## `NO_EXTERNAL_NAVIGATION_GATE`

- Purpose: block unapproved navigation.
- Blocks: external URLs, real page visits, navigation tests against public pages.
- Paths/keywords to watch: `http://`, `https://`, `Navigate`, `Goto`, URL open helpers.
- Expected evidence: no external navigation code or tests.
- PASS: no code navigates external pages.
- NO-GO: any block navigates externally without explicit approval.
- Suggested scan: `rg -n "https?://|Navigate|Goto|OpenUrl" src tests scripts`
- Report as: `NO_EXTERNAL_NAVIGATION_GATE=PASS/NO_GO`.

## `NO_REAL_ACTION_GATE`

- Purpose: ensure all actions remain fixture/in-memory.
- Blocks: real click, type, select, scroll, wait on live browser.
- Paths/keywords to watch: `ClickAsync`, `FillAsync`, `TypeAsync`, `SelectOptionAsync`, `Mouse`, `Keyboard`.
- Expected evidence: executor mode remains fixture-only.
- PASS: real action APIs are absent or explicitly blocked.
- NO-GO: any real page action can execute.
- Suggested scan: `rg -n "ClickAsync|FillAsync|TypeAsync|SelectOptionAsync|Mouse|Keyboard" src tests`
- Report as: `NO_REAL_ACTION_GATE=PASS/NO_GO`.

## `NO_PRODUCT_UI_ENABLEMENT_GATE`

- Purpose: prevent product UI from enabling browser actions.
- Blocks: buttons, toggles, commands, or menus that trigger live actions.
- Paths/keywords to watch: browser action labels, execute buttons, run actions, action gateway hooks.
- Expected evidence: no product UI files changed for action enablement.
- PASS: UI remains non-executing for browser automation.
- NO-GO: UI can start live browser automation.
- Suggested scan: `git diff --name-only -- browser-extension src | rg "(html|css|js|tsx)$"`
- Report as: `NO_PRODUCT_UI_ENABLEMENT_GATE=PASS/NO_GO`.

## `NO_SYSTEM_BROWSER_FALLBACK_GATE`

- Purpose: prevent fallback to installed Chrome, Edge, or default Chromium.
- Blocks: system executable paths, Playwright default browser, channel usage.
- Paths/keywords to watch: browser launch scripts, runtime config, `chrome`, `msedge`, `chromium`.
- Expected evidence: no forbidden browser usage.
- PASS: only pinned CloakBrowser runtime remains valid.
- NO-GO: system browser fallback exists.
- Suggested scan: `rg -n "chrome\\.exe|msedge\\.exe|chromium\\.exe|channel:\\s*['\\\"](chrome|msedge)['\\\"]|chromium\\.launch" .`
- Report as: `NO_SYSTEM_BROWSER_FALLBACK_GATE=PASS/NO_GO`.

## `NO_EXTENSION_DEFAULT_FALLBACK_GATE`

- Purpose: keep Chrome Extension legacy/no-default.
- Blocks: extension as runtime fallback or default proof.
- Paths/keywords to watch: extension runtime labels, sidepanel harness, fallback logic.
- Expected evidence: no-extension default harness remains the proof.
- PASS: extension remains legacy/compat-only.
- NO-GO: extension becomes default runtime or fallback.
- Suggested scan: `rg -n "extension.*default|fallback.*extension|installedSidepanelHarnessDefault" src tests scripts docs`
- Report as: `NO_EXTENSION_DEFAULT_FALLBACK_GATE=PASS/NO_GO`.

## `NO_STEALTH_CORE_TOUCH_GATE`

- Purpose: protect audited Stealth Core.
- Blocks: any change to protected paths.
- Paths/keywords to watch:
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
- Expected evidence: protected scope diff is empty.
- PASS: no protected path diff.
- NO-GO: any protected path changed.
- Suggested scan: `git diff --name-only -- stealth-engine/src/evasion stealth-engine/src/captcha stealth-engine/src/fingerprint stealth-engine/src/behavior stealth-engine/src/proxy stealth-engine/src/antiBlocking stealth-engine/src/handoff stealth-engine/src/StealthSession.js stealth-engine/src/StealthBrowserManager.js stealth-engine/src/index.js stealth-engine/tests/stealth-suite.test.js`
- Report as: `NO_STEALTH_CORE_TOUCH_GATE=PASS/NO_GO`.

## `NO_CREDENTIAL_OR_CHALLENGE_BYPASS_GATE`

- Purpose: prevent credential or challenge automation.
- Blocks: CAPTCHA solving, 2FA handling, anti-bot bypass, credential entry, login automation without explicit user action.
- Paths/keywords to watch: credential, password, token, OTP, 2FA, CAPTCHA, anti-bot, bypass, login.
- Expected evidence: human handoff tests and docs.
- PASS: these cases route to human handoff or blocked state.
- NO-GO: any automatic bypass or credential entry is introduced.
- Suggested scan: `rg -n "captcha|2fa|otp|credential|password|anti-bot|bypass|login" src tests docs`
- Report as: `NO_CREDENTIAL_OR_CHALLENGE_BYPASS_GATE=PASS/NO_GO`.

## `NO_LIVE_COLLECTOR_GATE`

- Purpose: prevent a live browser collector from being implemented under a fixture-safe or design-only block.
- Blocks: live DOM/AX/layout/screenshot/console/network collection without a new human decision, ADR, implementation prompt, and audit.
- Paths/keywords to watch: collector, CDP, DOM snapshot, AX tree, layout boxes, screenshot capture, console live, network live.
- Expected evidence: no collector code or live read path is introduced.
- PASS: collector remains fixture-safe or design-only.
- NO-GO: any code reads live browser state without explicit approval.
- Suggested scan: `rg -n "collector|DOM snapshot|AX tree|layout boxes|screenshot|console summary|network summary|CDP" src tests scripts docs`
- Severity if fails: P0.
- Report as: `NO_LIVE_COLLECTOR_GATE=PASS/NO_GO`.

## `NO_FILESYSTEM_LEAKAGE_GATE`

- Purpose: prevent local filesystem data from leaking into browser evidence or docs.
- Blocks: raw private paths, source content dumps, local artifact contents, and unredacted workspace paths.
- Paths/keywords to watch: drive roots, user profile paths, full local paths, file contents, evidence summaries.
- Expected evidence: path redaction and no raw local payloads in changed/new files.
- PASS: changed/new evidence is metadata-only and does not expose private local content.
- NO-GO: private paths or file contents are exposed without explicit redaction policy.
- Suggested scan: `rg -n "C:\\\\Users|C:\\\\DESARROLLO|/home/|/Users/|file contents|raw path" docs src tests`
- Severity if fails: P1, or P0 if secrets are exposed.
- Report as: `NO_FILESYSTEM_LEAKAGE_GATE=PASS/NO_GO`.

## `NO_PROMPT_INJECTION_WEB_GATE`

- Purpose: prevent page content from becoming policy, approval, or execution authority.
- Blocks: trusting DOM/page text as commands, gate overrides, approvals, or safety policy.
- Paths/keywords to watch: prompt injection, page instruction, web content, override, policy, approval, trust.
- Expected evidence: docs and code preserve policy outside page content.
- PASS: web content is treated as untrusted signal only.
- NO-GO: page content can authorize, bypass, or modify safety policy.
- Suggested scan: `rg -n "prompt injection|page instruction|web content|override.*policy|approval.*page" docs src tests`
- Severity if fails: P0 for executable policy bypass, P1 for ambiguous design.
- Report as: `NO_PROMPT_INJECTION_WEB_GATE=PASS/NO_GO`.

## `NO_SCREENSHOT_LEAKAGE_GATE`

- Purpose: prevent raw screenshots from leaking sensitive page data.
- Blocks: raw screenshot persistence, inline screenshots in evidence, unredacted visual payloads.
- Paths/keywords to watch: screenshot, image bytes, base64 image, visual capture, evidence pack.
- Expected evidence: screenshot metadata only unless a future approved live visual policy exists.
- PASS: only screenshot metadata is documented or stored.
- NO-GO: raw screenshots are collected or exposed without approval and redaction policy.
- Suggested scan: `rg -n "screenshot|base64|image bytes|visual capture" docs src tests artifacts`
- Severity if fails: P1, or P0 if secrets are exposed.
- Report as: `NO_SCREENSHOT_LEAKAGE_GATE=PASS/NO_GO`.

## `NO_SESSION_STORAGE_CAPTURE_GATE`

- Purpose: block collection of cookies, storage values, session tokens, or credential material.
- Blocks: cookie values, localStorage/sessionStorage values, session IDs, auth headers, bearer tokens.
- Paths/keywords to watch: cookie, localStorage, sessionStorage, storage value, authorization, bearer, session.
- Expected evidence: metadata counts or redacted keys only; no values.
- PASS: no storage/session values are captured.
- NO-GO: storage/session values are captured or serialized.
- Suggested scan: `rg -n "cookie|localStorage|sessionStorage|authorization|bearer|session" docs src tests`
- Severity if fails: P0 if raw secret/session values are exposed.
- Report as: `NO_SESSION_STORAGE_CAPTURE_GATE=PASS/NO_GO`.

## `NO_UI_WORDING_OVERCLAIM_GATE`

- Purpose: prevent UI or docs from implying live capabilities are enabled.
- Blocks: wording such as live ready, execute now, autonomous browser action, or productive automation enabled unless explicitly negated.
- Paths/keywords to watch: ready for live, execute live, automation enabled, run browser action, safe to execute.
- Expected evidence: product copy and docs state live remains blocked.
- PASS: wording is explicit that live/productive automation is disabled.
- NO-GO: wording implies live actions can be used.
- Suggested scan: `rg -n -i "ready for live|execute live|automation enabled|run browser action|safe to execute" docs src browser-extension`
- Severity if fails: P1, or P0 if it enables an action path.
- Report as: `NO_UI_WORDING_OVERCLAIM_GATE=PASS/NO_GO`.

## `NO_EVIDENCE_TAMPERING_GATE`

- Purpose: preserve evidence integrity for audits and product timeline use.
- Blocks: silent mutation, missing correlation IDs, untraceable evidence edits, or forged pass claims.
- Paths/keywords to watch: evidence, correlation, generatedAt, source, redaction status, pass.
- Expected evidence: evidence records include source, decision, redaction status, and validation references.
- PASS: evidence remains traceable and metadata-only.
- NO-GO: evidence can be changed or reported without traceable source or validation.
- Suggested scan: `rg -n "evidence|correlation|generatedAt|redaction|PASS|decision" docs src tests`
- Severity if fails: P1, or P0 if it fabricates safety pass.
- Report as: `NO_EVIDENCE_TAMPERING_GATE=PASS/NO_GO`.

## `NO_AUDIT_LOG_BYPASS_GATE`

- Purpose: ensure future live-relevant decisions and validations cannot bypass audit recording.
- Blocks: unlogged approvals, unlogged live actions, unlogged gate overrides, and missing validation evidence.
- Paths/keywords to watch: audit log, approval, gate override, validation, decision, bypass.
- Expected evidence: every future live-relevant gate reports command, result, and reviewed files.
- PASS: audit records remain mandatory for gate transitions.
- NO-GO: live-relevant decisions can occur without audit log or validation evidence.
- Suggested scan: `rg -n "audit log|approval|gate override|validation|decision|bypass" docs src tests`
- Severity if fails: P1, or P0 if a live action can bypass logging.
- Report as: `NO_AUDIT_LOG_BYPASS_GATE=PASS/NO_GO`.

## Reporting Format

Every future block must report:

- Gate name.
- Result: `PASS`, `BLOCKED`, or `NO_GO`.
- Evidence command.
- Files inspected.
- Exception classification if a keyword hit is allowed.

Any `NO_GO` result blocks commit and push.
