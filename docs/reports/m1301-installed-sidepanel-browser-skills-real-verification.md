# M1301 - Installed Sidepanel + Browser Skills Real Verification

## 1. Decision

**INSTALLED_SIDEPANEL_BROWSER_SKILLS_BLOCKED_WITH_EXACT_CAUSE**

The installed sidepanel and Browser Skills real flow were not verified. The blocking cause is environmental: the available Google Chrome build rejects command-line unpacked extension loading.

No product files were changed.

## 2. What Was Actually Verified

| Item | Result | Evidence |
|---|---|---|
| Worktree path | Verified | `C:\DESARROLLO\NodalOS\Codigo-m12-audit` |
| Branch | Verified | `chrome-lab-001-extension-local-ai-bridge` |
| Audit commit M1289 | Pushed | `e8b1b58..29c4783` pushed to origin |
| Manifest JSON parse | Verified | `manifest.json` parses and exposes expected fields |
| `side_panel.default_path` | Verified | `sidepanel.html` |
| Required permissions | Verified | `sidePanel`, `activeTab`, `tabs`, `scripting`, `storage` are present |
| Host permissions | Verified | `http://*/*`, `https://*/*` are present |
| Content script registration | Verified | `content_script.js` registered for http/https |
| JS syntax | Verified | `node --check browser-extension/onebrain-chrome-lab/sidepanel.js` passed |
| Chrome CDP startup | Verified | Chrome 149 started with remote debugging in a temp profile |
| NODAL OS unpacked load through CLI | Blocked | Chrome log: `--load-extension is not allowed in Google Chrome, ignoring.` |
| NODAL OS extension registration | Not verified | No NODAL OS service worker or extension target appeared after `--load-extension` |
| Real sidepanel container | Blocked | Extension did not load, so sidepanel could not be opened |
| Browser Skills real tab capture/index | Blocked | Extension did not load, so APIs could not run from installed sidepanel |

## 3. What Works In Installed Sidepanel

Nothing was verified inside the installed sidepanel in this block.

Do not count `file://sidepanel.html` or static code review as installed sidepanel verification. This block did not substitute local HTML for installed extension testing.

## 4. Exact Blocker

Automated load attempts used the required unpacked extension directory:

`C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`

Variants attempted:

| Variant | Result |
|---|---|
| `--load-extension=<path>` | Ignored by Chrome |
| `--disable-extensions-except=<path> --load-extension=<path>` | Ignored by Chrome |
| `--load-extension=<path>/` | Ignored by Chrome |

Chrome stderr included this exact line:

```text
--load-extension is not allowed in Google Chrome, ignoring.
```

Only built-in/component extension targets appeared, such as Google Hangouts and Google Network Speech. NODAL OS did not register.

## 5. What Was Adjusted

No code was adjusted.

Reason: the blocker is not a missing manifest permission or sidepanel code bug. The available Chrome binary rejects the command-line unpacked load method.

## 6. Requirement Matrix

| Requisito pedido | Estado | Evidencia | Archivo | Como probarlo |
|---|---|---|---|---|
| Cargar extension unpacked | BLOCKED | Chrome log says `--load-extension is not allowed in Google Chrome, ignoring.` | `browser-extension/onebrain-chrome-lab/manifest.json` | Use manual `chrome://extensions` load or a Chromium/Chrome for Testing build that allows unpacked CLI load. |
| Abrir sidepanel real | BLOCKED | NODAL OS extension did not register, so no real sidepanel target could open. | `browser-extension/onebrain-chrome-lab/sidepanel.html` | Load extension manually, click NODAL OS action, confirm sidepanel opens. |
| Mission Control visible in installed sidepanel | BLOCKED | Sidepanel real did not open. | `sidepanel.html`, `sidepanel.css`, `sidepanel.js` | After manual install, open sidepanel and inspect Mission Control. |
| Crear mision in installed sidepanel | BLOCKED | Depends on sidepanel real opening. | `sidepanel.js` | After manual install, create mission in sidepanel. |
| Run demo in installed sidepanel | BLOCKED | Depends on sidepanel real opening. | `sidepanel.js` | After manual install, click `Run demo`. |
| Browser Skills visible in installed sidepanel | BLOCKED | Depends on sidepanel real opening. | `sidepanel.html`, `sidepanel.js` | After manual install, scroll to Browser Skills. |
| Capturar pestana activa | BLOCKED | Requires installed extension context with `chrome.tabs.query`; extension did not load. | `sidepanel.js`, `manifest.json` | Open real sidepanel beside an http/https tab and click `Capturar pestana`. |
| Indexar pagina real | BLOCKED | Requires installed extension context with `chrome.scripting.executeScript`; extension did not load. | `sidepanel.js`, `manifest.json` | Open real sidepanel beside an http/https tab and click `Indexar pagina`. |
| Detectar elementos | BLOCKED | Indexing did not run from installed context. | `sidepanel.js` | Index a page with links/buttons/inputs/headings/forms. |
| Detectar friccion basica | BLOCKED | Indexing did not run from installed context. | `sidepanel.js` | Index a page containing login/captcha/blocked/session-expired markers. |
| Guardar snapshot | BLOCKED | Browser Skills did not run from installed context. | `sidepanel.js` | Capture/index and inspect Browser Skills history. |
| Mostrar historial | BLOCKED | Browser Skills did not run from installed context. | `sidepanel.html`, `sidepanel.js` | Capture/index and inspect `Historial snapshots`. |
| Copiar resumen Browser Skill | BLOCKED | No real snapshot was created from installed context. | `sidepanel.js` | Capture/index, then click `Copiar resumen Browser Skill`. |
| Persistir tras reload | BLOCKED | No installed-context data was created. | `sidepanel.js` | Capture/index, reload sidepanel, inspect history. |
| No BrowserAct dependency | IMPLEMENTED_VERIFIED | Dependency scan and product tests remain clean. | `tests/OneBrain.Safety.Tests/NativeBrowserSkillsProductM1269BTests.cs` | Run `dotnet test ... --filter "TestCategory=NativeBrowserSkillsProduct"`. |
| No copy burocratico protagonista | PARTIAL | Primary Mission Control/Browser Skills code is clean by tests; broad scan still finds `planned/policy/blocked by policy` in advanced runtime paths. | `sidepanel.js` | Keep advanced wording out of primary product UX. |

## 7. Validations

Automated validation commands run for this block:

| Command | Result |
|---|---|
| `node --check browser-extension/onebrain-chrome-lab/sidepanel.js` | PASS |
| Manifest parse / permission inspection | PASS |
| Chrome CDP temp-profile launch | PASS |
| CLI unpacked extension load | BLOCKED by Chrome |

Full build/test validation is recorded in the final response for this block.

## 8. Files Modified

Only this report was created:

- `docs/reports/m1301-installed-sidepanel-browser-skills-real-verification.md`

No product files were changed.

## 9. Commit / Push

M1289 audit commit was pushed before this verification:

```text
e8b1b58..29c4783 chrome-lab-001-extension-local-ai-bridge -> chrome-lab-001-extension-local-ai-bridge
```

This M1301 report should be committed and pushed after validation.

## 10. Recommended Next Block

**M1302-M1313 - Manual Chrome Extension Load + Installed Sidepanel Verification**

Goal:

1. User manually opens `chrome://extensions`.
2. User enables Developer Mode.
3. User clicks `Load unpacked`.
4. User selects `C:\DESARROLLO\NodalOS\Codigo-m12-audit\browser-extension\onebrain-chrome-lab`.
5. Verify whether Chrome accepts the extension manually.
6. If accepted, run Mission Control and Browser Skills installed sidepanel flow.
7. If rejected, capture Chrome's visible error and apply the minimal manifest/code fix.

Alternative:

Install or use a Chrome for Testing / Chromium build that permits command-line `--load-extension`, then rerun M1290-M1301.
