# M1313 - Manual Installed Sidepanel + Browser Skills Verification

## Decision

MANUAL_INSTALLED_SIDEPANEL_BROWSER_SKILLS_BLOCKED_WITH_EXACT_CAUSE

## What Was Verified Manually

The repository preflight was verified from the required worktree:

- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- HEAD: `83a9dd7d65274827679a6823c1098592a8389579`
- Upstream: `origin/chrome-lab-001-extension-local-ai-bridge`
- Origin alignment: up to date at preflight
- Remaining untracked files: the three pre-existing M1221 BrowserAct docs

The extension package was inspected:

- `manifest.json` is present and valid JSON.
- `side_panel.default_path` is `sidepanel.html`.
- `service_worker.js` is present.
- Relevant permissions exist: `sidePanel`, `activeTab`, `tabs`, `scripting`, `storage`.
- Host permissions exist for `http://*/*` and `https://*/*`.

Manual installed verification could not be completed from this agent session because both available UI automation paths failed before interaction with Chrome:

- Chrome control connector failed before opening or claiming a tab: `sandboxCwd must use the file URI scheme`.
- Windows Computer Use connector failed with the same environment metadata error: `sandboxCwd must use the file URI scheme`.

The previous automated method remains blocked by Google Chrome itself:

- Chrome log from M1301: `--load-extension is not allowed in Google Chrome, ignoring.`

Therefore, the required `chrome://extensions` manual load step still needs a human operator or a working GUI automation channel.

## What Works In The Installed Sidepanel

Not verified in an installed sidepanel in this block.

The product files and automated tests indicate the feature exists in code, but this report does not count that as installed verification.

## Exact Blocker

BLOCKED_BY_UNAVAILABLE_CHROME_UI_AUTOMATION

The task requires interacting with `chrome://extensions` and selecting the unpacked extension folder. In this session:

- The dedicated Chrome automation backend is unavailable due to environment metadata failure.
- The Windows UI automation backend is unavailable due to the same environment metadata failure.
- Google Chrome rejects command-line extension loading with `--load-extension`.

No installed-sidepanel claim is made.

## Adjustments

No product code was changed.

No manifest, service worker, Bridge/CSP, provider/cloud, runtime, PC Commander, or release/store files were changed.

## Requirement Matrix

| Requested requirement | Status | Evidence | File | How to test |
|---|---|---|---|---|
| Load unpacked extension manually | BLOCKED | Chrome/Windows UI automation unavailable; prior CLI load rejected by Chrome | `browser-extension/onebrain-chrome-lab/manifest.json` | In Chrome: `chrome://extensions` -> Developer mode -> Load unpacked -> select `browser-extension/onebrain-chrome-lab` |
| Open real sidepanel | BLOCKED | Extension was not manually loaded by this agent session | `browser-extension/onebrain-chrome-lab/sidepanel.html` | After loading extension, click NODAL OS extension action and open sidepanel |
| Mission Control visible | BLOCKED | Requires installed sidepanel | `browser-extension/onebrain-chrome-lab/sidepanel.html` | Open installed sidepanel and verify Mission Control renders |
| Create mission | BLOCKED | Requires installed sidepanel flow | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Use Nueva mision form in installed sidepanel |
| Edit mission | BLOCKED | Requires installed sidepanel flow | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Edit an existing mission in installed sidepanel |
| Run demo | BLOCKED | Requires installed sidepanel flow | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Click Run demo in installed sidepanel |
| Run history | BLOCKED | Requires installed sidepanel flow | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Run demo and open Historial |
| Browser Skills visible | BLOCKED | Requires installed sidepanel | `browser-extension/onebrain-chrome-lab/sidepanel.html` | Open Browser Skills section in installed sidepanel |
| Capture active tab | BLOCKED | Requires installed sidepanel and Chrome extension APIs | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Open an `http/https` tab, return to sidepanel, click Capturar pestana |
| Index real page | BLOCKED | Requires installed sidepanel and `chrome.scripting.executeScript` | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Click Indexar pagina after active tab capture |
| Detect elements | BLOCKED | Depends on real page indexing | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Verify links/buttons/inputs/headings/forms counts |
| Detect basic friction | BLOCKED | Depends on real page indexing | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Test on a page with login/captcha/error text |
| Save snapshot | BLOCKED | Depends on Browser Skills capture/indexing | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Capture/index page and verify snapshot history |
| Show snapshot history | BLOCKED | Depends on snapshot creation | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Select a previous snapshot from Browser Skills history |
| Copy Browser Skill summary | BLOCKED | Depends on Browser Skills evidence state | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Click Copiar resumen Browser Skill |
| Persist after reload | BLOCKED | Depends on installed sidepanel storage behavior | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Reload sidepanel and verify missions/runs/snapshots remain |
| No BrowserAct dependency | IMPLEMENTED_VERIFIED | Dependency scan found no BrowserAct package reference | project files | Run BrowserAct dependency scan |
| No bureaucratic copy as protagonist | PARTIAL | Product tests cover Browser Skills primary copy; legacy wording remains in non-primary/advanced paths | `browser-extension/onebrain-chrome-lab/sidepanel.js` | Run bad UX wording scan and inspect primary Mission Control UI |

## Validations

- `dotnet build .\OneBrain.slnx --no-restore`: PASS, with existing warnings only.
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`: PASS.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: PASS, 17 passed.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NativeBrowserSkillsProduct"`: PASS, 6 passed.
- `git diff --check`: PASS.
- Secret scan on this report: PASS.
- BrowserAct dependency scan: PASS, no package dependency reference found.
- Bad UX wording scan: PARTIAL. Existing `planned` and `blocked by policy` strings remain in legacy JavaScript paths; no product code was changed in this block.

## Modified Files

- `docs/reports/m1313-manual-installed-sidepanel-browser-skills-verification.md`

## Commit / Push

This report is the only tracked change in this block and is intended to be committed with:

`docs: record manual installed sidepanel verification`

The final pushed commit hash is recorded in the final response.

## Recommended Next Block

M1314-M1325 - Human-Assisted Chrome Extension Load Verification

Recommended operator step:

1. Open Chrome normally.
2. Navigate to `chrome://extensions`.
3. Enable Developer mode.
4. Click Load unpacked.
5. Select `C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`.
6. Confirm NODAL OS appears without manifest or service worker errors.
7. Then run installed sidepanel Mission Control and Browser Skills verification.
