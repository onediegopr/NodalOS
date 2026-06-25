# CloakBrowser CDP Direct Runtime Migration

Decision: NODAL OS keeps the current Chrome Extension product UI working, but the next primary browser runtime path is `cloakbrowser` with direct CDP.

Current state:

* `browser-runtime.lock.json` pins the intended provider as `cloakbrowser`.
* CDP direct mode is the default runtime direction.
* System Chrome, Edge, Chromium, Playwright default Chromium, and silent browser fallback are rejected by guard code.
* Live CDP launch is blocked until a real CloakBrowser fork/release/package/binary artifact is provided.
* Chrome Extension remains legacy UI / validation surface and is not the default future runtime path.

Migration matrix:

| Current extension surface | CDP direct target |
| --- | --- |
| Background/service worker | NODAL OS desktop/runtime controller |
| Content scripts | `CdpInjectionManager` |
| Extension/app messaging | CDP bindings / local EventBus future |
| Sidepanel extension | NODAL OS Desktop UI future |
| DOM access | CDP Runtime / DOM / Page |
| Network observer | CDP Network / Fetch |
| Screenshots/evidence | CDP `Page.captureScreenshot` |
| Tabs/windows | CDP Target domain |
| Browser lifecycle | CloakBrowser supervisor |

Live runtime requirement:

Provide a real CloakBrowser runtime artifact and update the lock metadata:

* `runtime_version`
* `runtime_commit`
* `upstream_commit`
* `binary_sha256`

Local runtime path configuration is intentionally not committed. Use either:

* `NODAL_CLOAKBROWSER_RUNTIME_PATH`
* `.local/browser-runtime.local.json`

Example local config:

```json
{
  "cloakbrowser_executable_path": "D:\\runtimes\\cloakbrowser\\cloakbrowser.exe",
  "cdp_port": "ephemeral-or-reserved"
}
```

Until then, live CDP healthcheck status is `BLOCKED_RUNTIME_ARTIFACT_REQUIRED`.
