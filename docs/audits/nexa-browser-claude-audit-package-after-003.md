# NEXA Browser Claude Audit Package After Browser-003

Date: 2026-06-14
Branch: `chrome-lab-001-extension-local-ai-bridge`
Base commit: `e26596d3bc7bf40ed6d881d0265b04e6e154f561`

## Purpose

This package summarizes the state of NEXA Chrome Operator Lab after Browser-003 and the Browser-003.5 real Chrome smoke gate.

The lab is intentionally separate from the Windows/UIA engine. This audit package does not cover safe.click, safe.read, safe.type, UIA identity, or Windows automation.

## Architecture Summary

NEXA Chrome Operator Lab consists of:

- Local .NET bridge: `src/OneBrain.ChromeLab.Bridge`
- Chrome MV3 extension: `browser-extension/onebrain-chrome-lab`
- Side panel UI
- Background service worker
- Content script tools
- WebSocket bridge protocol
- HTTP control endpoints

Bridge endpoints:

- `GET /health`
- `GET /config/public`
- `POST /api/runs`
- `POST /api/runs/{runId}/stop`
- `POST /api/runs/{runId}/pause`
- `POST /api/runs/{runId}/resume`
- `WS /ws/extension`

Extension tools include:

- `observePage`
- `getElementCatalog`
- `resolveTarget`
- `clickElement`
- `setElementValue`
- `focusElement`
- `readElement`
- `highlightElement`
- `scrollElementIntoView`
- selector legacy tools retained for compatibility

## Browser-001 State

Browser-001 added the browser-first Chrome Lab:

- Local bridge
- MV3 extension
- Side panel
- WebSocket protocol
- Basic observe/decide/act loop
- OpenAI provider from bridge only
- No API key in extension
- STOP, pause, resume
- Credential/captcha/2FA pause policy

## Browser-002 State

Browser-002 reorganized the extension UI into product modes:

- `Operar`
- `Aprender`
- `Recetas`
- `Runtime`

It added:

- NEXA branding
- Persistent STOP
- Human intervention banner
- Learning Mode V0
- Recipe storage V0
- Runtime diagnostics
- Cleaner operator target/verification cards

## Browser-003 State

Browser-003 added deterministic recipe foundations:

- Recipe Schema V1 in `recipe_core.js`
- Learning to Recipe V1 conversion
- Deterministic service-worker recipe runner
- Step state with `recipeRunId`, `currentStepIndex`, `currentStepId`, `status`, `stepResults`
- Supported steps:
  - `navigate`
  - `observe`
  - `resolveTarget`
  - `click`
  - `input`
  - `select`
  - `wait`
  - `verify`
  - `humanCheckpoint`
- Resume from `humanCheckpoint` reobserves page before continuing.
- Basic recovery:
  - retry current step
  - abort
  - skip only safe steps (`observe`, `wait`, `verify`)
- JS fixture tests for catalog/scoring/selector/recipe basics.

Browser-003 also fixed a target rehydration weakness:

- `content_script.js` can now rehydrate by `stableSelectors` even when there is no live `elementId`.

## Browser-003.5 Smoke Environment

Bridge started on:

```text
http://127.0.0.1:8787
```

Fixture server started on:

```text
http://127.0.0.1:8899
```

Local fixture pages:

- `buttons.html`
- `form.html`
- `checkpoint.html`

These fixture pages are under ignored `artifacts/` and are not committed.

## Smoke Results

### Smoke 1 - Runtime Basic

Result: Partial pass.

Verified:

- Bridge process starts.
- `GET /health` returns OK.
- `GET /config/public` returns protocol/model/public config.
- API key is not exposed.
- `ApiKey.txt` remains ignored.
- No real API key found in versioned files.

Blocked:

- `POST /api/runs` returned:

```json
{
  "status": "error",
  "message": "No extension client connected."
}
```

Conclusion:

The bridge is healthy, but the MV3 client was not connected during automated smoke. Runtime UI connection status could not be visually verified through Chrome automation because the side panel is not exposed as a normal browser tab.

### Smoke 2 - Operator Mode Simple

Result: Blocked at extension connection.

Verified externally in real Chrome:

- Local page `buttons.html` loads.
- DOM contains:
  - `Ingresar`
  - `Iniciar sesion`
  - `Registrarse`

Blocked:

- NEXA Operator run could not execute because bridge reported no extension client connected.
- Therefore `observePage`, `resolveTarget`, `clickElement`, candidates, score, verification, and timeline could not be validated through the live extension.

### Smoke 3 - Non-sensitive Form

Result: Blocked at extension connection.

Fixture page exists and loads over HTTP, but extension-driven observation/input could not be validated because no extension client was connected.

Expected checks remain:

- Detect non-sensitive inputs.
- Redact/avoid password value.
- Block password field input.
- Preserve redacted logs.

### Smoke 4 - Learning Mode

Result: Blocked at side panel accessibility/extension connection.

Learning Mode requires the side panel UI and service worker connection. The side panel was not accessible as a browser tab and the bridge had no connected extension client.

### Smoke 5 - Recipe Runner

Result: Blocked at extension connection.

Browser-003 automated tests validate Schema V1 and runner code presence, but live execution of a saved recipe could not be run because the extension client was not connected.

### Smoke 6 - Human Checkpoint

Result: Blocked at extension connection.

The code path exists and Browser-003 documents resume reobserve behavior, but live UI validation was blocked by missing extension client connection.

### Smoke 7 - AFIP/ARCA Initial

Result: Partial pass.

Verified in real Chrome:

- ARCA page was open at:

```text
https://www.afip.gob.ar/landing/default.asp
```

- DOM contained a unique accessible link:

```text
Iniciar sesión
```

- The link href was:

```text
https://auth.afip.gob.ar/contribuyente_/login.xhtml
```

- Navigating to that href reached:

```text
https://auth.afip.gob.ar/contribuyente_/login.xhtml
```

- Login/credential signals were visible:
  - `CUIT/CUIL`
  - `Clave Fiscal`
  - `Siguiente`

Not performed:

- No credentials entered.
- No captcha bypass.
- No fiscal action.
- No comprobante emission.

Blocked:

- NEXA extension-driven AFIP flow could not be validated because the extension client was not connected.

## Bugs Found

### B-003.5-1 - Extension client not connected during real smoke

Severity: Blocking for live end-to-end smoke.

Evidence:

`POST /api/runs` returned `No extension client connected`.

Impact:

- Operator Mode cannot start.
- Learning Mode cannot be validated live.
- Recipe Runner cannot execute live.
- Runtime side panel WebSocket state cannot be confirmed from bridge.

Likely causes to investigate:

- Extension side panel was not open.
- Service worker was suspended.
- Extension was not loaded or not connected to `ws://127.0.0.1:8787/ws/extension`.
- Side panel connection state does not auto-recover when panel is closed/reopened.
- The current architecture depends on manual side panel connect before `/api/runs`.

### B-003.5-2 - Side panel is not automatable as a normal Chrome tab

Severity: Medium for automated smoke.

Impact:

Chrome automation can inspect web pages, but not the extension side panel as a regular page. Full UI smoke still needs either:

- manual user-driven verification,
- a dedicated test harness page,
- extension-level test hooks,
- or a documented manual procedure.

### B-003.5-3 - AFIP click did not navigate during external browser automation

Severity: Low/uncertain.

Evidence:

The DOM exposed one `Iniciar sesión` link with correct href. The external smoke click did not change URL, but direct navigation to the href worked.

Impact:

This may be an artifact of the Chrome automation layer, not necessarily NEXA. It still confirms that target resolution should have enough semantic information for the link.

## Bugs Corrected In Browser-003.5

No product code bug was corrected in Browser-003.5.

Reason:

The only blocking failure observed was missing live extension connection. Fixing that may require architecture or UX changes larger than the allowed Browser-003.5 scope.

## Validations Run

Previously validated at Browser-003 close:

- JS syntax checks for extension scripts.
- JS fixture tests.
- `.NET build`.
- `.NET tests`.
- Bridge self-test.
- Secret scan.

Browser-003.5 validation:

- Bridge health OK.
- Bridge public config OK.
- Secret scan over versioned browser/bridge/docs files OK.
- Real Chrome ARCA read-only observation OK.
- Real Chrome AFIP/ARCA credential page observation OK.

Final build/test should be rerun after this audit document is committed.

## Current Risks

1. End-to-end operation depends on a connected side panel/service worker, but the bridge currently has no diagnostic endpoint showing connected clients.
2. There is no automated extension UI harness, so side panel smoke remains manual.
3. Recipe Runner V1 has no persistent recovery after service worker restart.
4. Operator Mode with OpenAI cannot be validated without a local key, but key handling remains intentionally local and ignored.
5. Real-world pages may require stronger post-click verification and candidate repair UI.

## Questions For Claude

1. Should Browser-004 prioritize extension connection reliability before candidate repair?
2. Should the bridge expose a non-sensitive `/debug/runtime` endpoint with connected client count and last hello timestamp?
3. Should the extension auto-connect on install/startup, or only when side panel opens?
4. Should Side Panel Runtime include a force reconnect button and a connection event timeline?
5. What is the safest way to create an automated side panel smoke harness without using Playwright/CDP/product desktop automation?
6. Should Recipe Runner state be persisted in `chrome.storage.session` or `chrome.storage.local` to survive service worker suspension?
7. Should Operator Mode refuse `Start Run` locally when bridge health is OK but WS client is not connected, with a clearer UI reason?

## Recommended Next Hitos

Preferred:

```text
NEXA Browser-004 - Extension Connection Reliability + Runtime Debug Endpoint
```

Then:

```text
NEXA Browser-005 - Candidate Repair UI + Persistent Recipe Run Recovery
```

Alternative if Claude accepts the current connection model:

```text
NEXA Browser-004 - Candidate Repair UI + Persistent Recipe Run Recovery
```

## Audit Recommendation

Run Claude audit now before adding more features.

Reason:

The core Browser-001/002/003 architecture exists, but Browser-003.5 found a blocking operational gap: live smoke cannot proceed unless extension connection lifecycle is made reliable and observable.
