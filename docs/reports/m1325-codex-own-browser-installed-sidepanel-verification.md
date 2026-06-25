# M1325 - Codex-Owned Browser Installed Sidepanel Verification

Date: 2026-06-25
Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
Branch: `chrome-lab-001-extension-local-ai-bridge`
Expected HEAD at start: `2eacf674a256709ace9e2fb6e951cc106a46300a`

## 1. Decision

`CODEX_OWN_BROWSER_INSTALLED_SIDEPANEL_BROWSER_SKILLS_VERIFIED`

The installed NODAL OS extension sidepanel was verified through a Codex-controlled Chromium route, not through `file://sidepanel.html` and not by repeating the previously blocked Google Chrome stable path.

Status: `IMPLEMENTED_VERIFIED`

## 2. Detected Browsers

| Candidate | Detected | Result |
| --- | --- | --- |
| Google Chrome stable | Yes: `C:\Program Files\Google\Chrome\Application\chrome.exe` | Not used as primary route because the prior known blocker was Google Chrome ignoring `--load-extension`. |
| Chrome for Testing | No: `C:\Program Files\Google\Chrome for Testing\Application\chrome.exe` | `NOT_DONE` |
| Microsoft Edge | No: `C:\Program Files\Microsoft\Edge\Application\msedge.exe` | `NOT_DONE` |
| Playwright bundled Chromium | Yes: `C:\Users\diego\AppData\Local\ms-playwright\chromium-1223\chrome-win64\chrome.exe` | Used and verified. Browser reported `Chrome/148.0.7778.96`. |

## 3. Routes Attempted

| Route | Status | Evidence |
| --- | --- | --- |
| Stable Google Chrome with `--load-extension` | `NOT_DONE` | Prior block already recorded exact cause: `--load-extension is not allowed in Google Chrome, ignoring.` This audit did not burn cycles repeating that route. |
| Playwright bundled Chromium with temporary profile, `--disable-extensions-except`, `--load-extension`, CDP | `IMPLEMENTED_VERIFIED` | CDP target found for `chrome-extension://lhkneedkhndfkjefmdgcojadepjcfplp/service_worker.js`; runtime manifest name `NODAL OS`; sidepanel opened at `chrome-extension://lhkneedkhndfkjefmdgcojadepjcfplp/sidepanel.html`. |
| Differential minimal unpacked extension on same Chromium route | `IMPLEMENTED_VERIFIED` | Minimal MV3 extension loaded with service worker target. This proved the route supports unpacked extensions. |
| CDP filtering by first `chrome-extension://` target | `PARTIAL` | Initial run selected Chromium component extension `Google Hangouts` at `chrome-extension://nkeimhogjdpnpccoofpliimaahmaaome/thunk.js`; fixed by selecting manifest name `NODAL OS`. |

## 4. What Was Actually Verified

- Extension was installed as an unpacked MV3 extension in a temporary Chromium profile.
- NODAL OS service worker target existed under a real `chrome-extension://` origin.
- Runtime manifest was read from the installed extension context with `chrome.runtime.getManifest()`.
- Installed sidepanel page loaded from `chrome-extension://lhkneedkhndfkjefmdgcojadepjcfplp/sidepanel.html`.
- Sidepanel had extension APIs available: `chrome.runtime`, `chrome.tabs`, `chrome.scripting`, `chrome.sidePanel`, `chrome.storage`.
- Mission Control flow worked inside the installed extension page:
  - created mission `M1325 Installed Sidepanel Mission`;
  - edited mission path was exercised;
  - ran demo;
  - rendered run note `Installed sidepanel note from M1325 harness`;
  - copy report action returned without harness exception.
- Browser Skills flow worked against an active local HTTP tab:
  - captured active tab `http://127.0.0.1:<port>/fixture`;
  - indexed DOM with `chrome.scripting.executeScript`;
  - rendered title `M1325 Browser Skills Fixture`;
  - rendered `8` indexed elements;
  - detected friction signal `Captcha visible`;
  - copied Browser Skill summary without harness exception.

Primary evidence artifact, not committed: `.codex-tmp/m1325-chromium-result.json`.

## 5. What Works

- Codex-owned Chromium can load the local NODAL OS unpacked extension when launched directly from the Playwright browser cache.
- The installed extension path is observable through CDP and must be selected by runtime manifest, because Chromium component extensions are also exposed as `chrome-extension://` targets.
- The installed sidepanel can exercise UI storage, mission demo state, tab capture, page indexing, friction detection, and summary copy paths.

## 6. Blockers

No current blocker for installed sidepanel verification.

Historical blocker still stands for the old route:

`Google Chrome stable rejected unpacked extension launch flags: --load-extension is not allowed in Google Chrome, ignoring.`

## 7. Adjustments

- Did not modify product runtime code.
- Did not add BrowserAct, stealth, proxy, captcha solving, or runtime integration.
- Adjusted harness selection from "first extension target" to "target whose runtime manifest name is `NODAL OS`".
- Adjusted harness assertions to match actual sidepanel storage shape:
  - Mission note is rendered in run history, not as a top-level `mission.notes` array.
  - Browser Skills snapshot store keeps `snapshots` and rendered DOM summary carries the derived element count.

## 8. Requirement Matrix

| Requirement | Status | Evidence |
| --- | --- | --- |
| Use mandatory worktree | `IMPLEMENTED_VERIFIED` | `C:\DESARROLLO\NodalOS\Codigo-m12-audit` |
| Use mandatory branch | `IMPLEMENTED_VERIFIED` | `chrome-lab-001-extension-local-ai-bridge` |
| Confirm expected HEAD | `IMPLEMENTED_VERIFIED` | `2eacf674a256709ace9e2fb6e951cc106a46300a` at preflight |
| Do not repeat blocked Google Chrome path as only path | `IMPLEMENTED_VERIFIED` | Used Playwright bundled Chromium route. |
| Try Codex-owned browser route | `IMPLEMENTED_VERIFIED` | Playwright bundled Chromium executable, temporary profile, CDP. |
| Verify installed extension, not `file://sidepanel.html` | `IMPLEMENTED_VERIFIED` | Sidepanel URL was `chrome-extension://lhkneedkhndfkjefmdgcojadepjcfplp/sidepanel.html`. |
| Verify runtime manifest | `IMPLEMENTED_VERIFIED` | Runtime manifest name `NODAL OS`; side panel `sidepanel.html`; service worker `service_worker.js`. |
| Verify Mission Control principal | `IMPLEMENTED_VERIFIED` | Mission create/edit/run/note/copy flow exercised in installed extension page. |
| Verify Browser Skills | `IMPLEMENTED_VERIFIED` | Active tab captured; page indexed; 8 elements rendered; captcha friction detected. |
| Avoid productive integration | `IMPLEMENTED_VERIFIED` | Report-only change; no runtime/product code changed. |
| Confirm M1221 files remain present | `IMPLEMENTED_VERIFIED` | Untracked docs exist for M1221 ADR/report/roadmap; not included in this commit. |

## 9. Validations

| Command | Status | Result |
| --- | --- | --- |
| `dotnet build .\OneBrain.slnx --no-restore` | `IMPLEMENTED_VERIFIED` | Passed. Warnings only, including preview .NET SDK notice and existing obsolete/nullability analyzer warnings. |
| `node --check browser-extension/onebrain-chrome-lab/sidepanel.js` | `IMPLEMENTED_VERIFIED` | Passed. |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"` | `IMPLEMENTED_VERIFIED` | Passed: 17 passed, 0 failed. |
| `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NativeBrowserSkillsProduct"` | `IMPLEMENTED_VERIFIED` | Passed: 6 passed, 0 failed. |
| Installed extension harness | `IMPLEMENTED_VERIFIED` | Passed all nine checks in `.codex-tmp/m1325-chromium-result.json`. |
| `git diff --check` | `IMPLEMENTED_VERIFIED` | Passed. |
| Secret scan on changed M1325 report | `IMPLEMENTED_VERIFIED` | No credential patterns found. |
| Dependency manifest scan for BrowserAct/stealth/proxy/captcha-solver packages | `IMPLEMENTED_VERIFIED` | No package references found. |
| Scoped sidepanel wording scan | `IMPLEMENTED_VERIFIED` | No bad UX wording patterns found in `sidepanel.html` or `sidepanel.js`. |
| Scoped BrowserAct text scan | `IMPLEMENTED_VERIFIED` | Found only intentional UI/report references such as `BrowserAct: referencia externa no usada en runtime`; no runtime dependency was added. |

## 10. Files Modified

- `docs/reports/m1325-codex-own-browser-installed-sidepanel-verification.md`

No extension runtime files were modified.

## 11. Commit And Push

Planned commit message:

`test: verify installed sidepanel with codex controlled browser`

Push target:

`origin chrome-lab-001-extension-local-ai-bridge`

## 12. Next Block Recommended

Run a follow-up block that turns the ad hoc CDP harness into a safe, repo-owned, non-product verification script under a test or tooling boundary. Keep it audit-only unless the main NODAL OS chat approves integration. The script should:

- select extension targets by runtime manifest instead of first `chrome-extension://` target;
- persist a redacted evidence JSON artifact;
- support Playwright bundled Chromium first, with Chrome for Testing/Edge fallbacks only when detected;
- fail closed if the loaded manifest is not `NODAL OS`;
- remain explicitly separate from BrowserAct, stealth, proxy, or captcha-solving runtime work.
