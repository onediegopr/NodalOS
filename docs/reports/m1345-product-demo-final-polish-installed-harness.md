# M1345 - Product Demo Final Polish Using Installed Sidepanel Harness

## Decision

PRODUCT_DEMO_FINAL_POLISH_INSTALLED_HARNESS_READY

## What Was Polished

- Kept Mission Control and Browser Skills product behavior unchanged.
- Cleaned visible technical planning copy in `sidepanel.js`:
  - replaced `blocked by policy` fallback with `requiere revision`;
  - replaced `policy blocker` style next-action copy with product-oriented review text;
  - replaced visible `Planned` labels with `Por hacer`;
  - localized several plan/recovery labels that can surface in technical timelines.

No new feature, gate, governance pack, runtime behavior, provider/cloud call, PC Commander path, BrowserAct dependency, stealth/proxy/CAPTCHA solver, manifest permission, or release/store path was added.

## What Stayed The Same

- Main Mission Control layout.
- Browser Skills UI and behavior.
- Installed sidepanel harness.
- Chrome extension manifest and permissions.
- Service worker, content script, Bridge/CSP, provider/cloud, runtime, and PC Commander code.

## Harness Result

Baseline run before changes:

- Status: PASS
- Browser: Playwright bundled Chromium `Chrome/148.0.7778.96`
- Extension registered: true
- `chrome.*` APIs available: true
- Mission Control flow: PASS
- Browser Skills flow: PASS
- Indexed elements: 9
- Friction summary: `Captcha visible, Login requerido`

Final run after cleanup:

- Status: PASS
- Decision: `INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_PASS`
- Evidence generated locally under `artifacts/local-verification/`

## Local Cleanup

- Removed `.codex-tmp/` local evidence directory.
- Removed three untracked M1221 BrowserAct docs because they were obsolete local history and not active product roadmap:
  - `docs/adr/browseract-provider-candidate-design-only-m1221.md`
  - `docs/reports/m1221-browseract-skills-fit-gap-stealth-proxy-captcha-core-capability-audit.md`
  - `docs/roadmap/browseract-provider-candidate-roadmap-m1221.md`

Generated harness evidence remains local under ignored `artifacts/`.

## Validation

- `node scripts/verify-installed-sidepanel.mjs`: PASS
- `node --check scripts/verify-installed-sidepanel.mjs`: PASS
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`: PASS
- `dotnet build .\OneBrain.slnx --no-restore`: PASS
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: PASS, 17 passed
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NativeBrowserSkillsProduct"`: PASS, 6 passed
- `git diff --check`: PASS
- Secret scan on changed files: PASS
- BrowserAct dependency scan: PASS, no package dependency reference found
- Bad UX wording scan: PASS for removed visible `blocked by policy`; remaining matches are internal status keys/normalizers (`planned`, `policySummary`, `execution-blocked-by-policy`) and not Mission Control or Browser Skills protagonist copy.

## Modified Files

- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `docs/reports/m1345-product-demo-final-polish-installed-harness.md`

## Next Block

Workspace Open real + Project Understanding visible read-only.

