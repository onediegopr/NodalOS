# M1333 - Installed Sidepanel Verification Harness

## Decision

INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_READY

## Script

Repo-owned script:

```powershell
node scripts/verify-installed-sidepanel.mjs
```

Optional override if Chromium is outside the Playwright cache:

```powershell
$env:NODAL_CHROMIUM_PATH='C:\path\to\chrome.exe'
node scripts/verify-installed-sidepanel.mjs
```

There is no root `package.json`, so no npm script was added.

## What It Verifies

- Locates Playwright bundled Chromium.
- Launches Chromium with a temporary profile.
- Loads the unpacked extension from `browser-extension/onebrain-chrome-lab`.
- Uses `--disable-extensions-except` and `--load-extension`.
- Discovers the installed NODAL OS extension service worker through CDP.
- Reads the runtime manifest and confirms `NODAL OS`.
- Opens `chrome-extension://<extension-id>/sidepanel.html`.
- Verifies `chrome.runtime`, `chrome.tabs`, `chrome.scripting`, `chrome.sidePanel`, and `chrome.storage`.
- Runs Mission Control create/edit/run/note/report flow.
- Runs Browser Skills active-tab capture and DOM indexing on a local HTTP fixture.
- Confirms indexed elements and friction detection.
- Generates redacted JSON evidence.

## Evidence Output

Default local output:

```text
artifacts/local-verification/installed-sidepanel-<timestamp>.redacted.json
```

`artifacts/` is gitignored, so generated evidence is local and not versioned.

The JSON includes:

- status
- browser used
- extension registered state
- redacted extension id
- API availability
- Mission Control flow result
- Browser Skills result
- element count
- friction summary
- redacted errors
- timestamp

## What It Does Not Touch

- Mission Control UX
- product runtime
- provider/cloud
- PC Commander
- BrowserAct
- stealth/proxy/captcha solver
- Chrome permissions
- manifest/service worker/product files
- release/store

## Run Result

Latest harness run in this block:

- Status: PASS
- Decision: `INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_PASS`
- Browser: Playwright bundled Chromium, `Chrome/148.0.7778.96`
- Extension registered: true
- Sidepanel loaded as installed extension page: true
- `chrome.*` APIs available: true
- Mission Control flow: PASS
- Browser Skills capture/index flow: PASS
- Indexed elements: 9
- Friction summary: `Captcha visible, Login requerido`
- Snapshot history generated: true

## Validations

- `node --check scripts/verify-installed-sidepanel.mjs`: PASS
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`: PASS
- `node scripts/verify-installed-sidepanel.mjs`: PASS
- `dotnet build .\OneBrain.slnx --no-restore`: PASS, with existing warnings only.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: PASS, 17 passed.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NativeBrowserSkillsProduct"`: PASS, 6 passed.
- `git diff --check`: PASS.
- Secret scan on changed files: PASS.
- BrowserAct dependency scan: PASS, no package dependency reference found.

## Modified Files

- `scripts/verify-installed-sidepanel.mjs`
- `docs/reports/m1333-installed-sidepanel-verification-harness.md`

## Commit / Push

Pending at report creation. Final commit hash is recorded in the final response.

## Next Recommended Block

M1334-M1345 - Product Demo Final Polish Using Repo-Owned Installed Verification Harness
