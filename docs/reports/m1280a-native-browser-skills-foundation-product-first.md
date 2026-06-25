# M1280A - Native Browser Skills Foundation Product-First

## 1. Decision

`NATIVE_BROWSER_SKILLS_FOUNDATION_READY_PRODUCT_FIRST_NO_BROWSERACT_RUNTIME`

- BrowserAct runtime: no integrado.
- BrowserAct dependency: no agregada.
- BrowserAct patterns: referencia.
- NODAL OS Native Browser Skills: foundation minima.
- Stealth / Proxy / Captcha: roadmap futuro, no runtime.

## 2. What Was Implemented

Added native descriptor contracts in `OneBrain.BrowserExecutor.Contracts`:

- `BrowserSkillManifest`
- `BrowserSkillCapabilityEnvelope`
- `BrowserStateSnapshot`
- `BrowserIndexedElement`
- `BrowserSkillSessionDescriptor`
- `BrowserSessionResilienceReport`
- `AccessFrictionEvent`
- `BlockedFlowRecoveryPlan`
- `HumanTakeoverRequest`
- `NetworkEvidenceCandidate`
- `CdpOperationCandidate`
- `StealthProfile`
- `ProxyRouteProfile`
- `CaptchaChallengeEvent`
- `CaptchaHandlingStrategy`

These models describe future browser skills. They do not execute browser actions.

## 3. Product Visibility

Mission Control principal remains focused on the local demo.

Modo avanzado now includes a compact Browser Skills card:

- Native browser skills: planned
- CDP skills: planned
- BrowserAct: reference only
- Stealth / Proxy / Captcha: future descriptors
- Runtime: not active

Copy shown:

- "Base futura para habilidades de navegador."
- "Sin runtime activo en esta demo."

## 4. Existing M1221 Intake

Existing M1221 docs were found and reviewed:

- `docs/reports/m1221-browseract-skills-fit-gap-stealth-proxy-captcha-core-capability-audit.md`
- `docs/adr/browseract-provider-candidate-design-only-m1221.md`
- `docs/roadmap/browseract-provider-candidate-roadmap-m1221.md`
- `artifacts/agent-operations/m1221/browseract-capability-envelope-scaffold.json`

Summary:

- BrowserAct remains useful as a pattern reference.
- BrowserAct should not become the current runtime.
- Stealth, proxy and captcha features are high-risk future descriptors only.
- NODAL OS keeps Chrome/CDP as the native product foundation.

## 5. Future

Future work may turn descriptors into product-facing Browser Skills if it directly supports the visible demo.

Not implemented now:

- BrowserAct install
- BrowserAct CLI/API/MCP calls
- stealth runtime
- proxy runtime
- CAPTCHA solving
- autonomous productive browser runtime
- provider/cloud calls
- release/store work

## 6. Validations

Executed validation set:

- `dotnet build .\OneBrain.slnx --no-restore`: PASS with existing legacy warnings.
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`: PASS.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --filter "FullyQualifiedName~NativeBrowserSkillsFoundationM1269ATests"`: PASS, 8/8.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`: PASS, 17/17.
- `git diff --check`: PASS with LF/CRLF normalization warnings.
- secret scan simple: PASS.
- BrowserAct dependency absence scan: PASS.
- Mission Control principal UX scan: PASS.

## 7. Modified Files

- `src/OneBrain.BrowserExecutor.Contracts/NativeBrowserSkillsContracts.cs`
- `tests/OneBrain.Safety.Tests/NativeBrowserSkillsFoundationM1269ATests.cs`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `docs/roadmap/native-browser-skills-product-roadmap.md`
- `docs/reports/m1280a-native-browser-skills-foundation-product-first.md`

## 8. Next Step

PAUSA BROWSER SKILLS TERMINADA — VOLVEMOS A PRODUCT DEMO / SIDEPANEL REAL

Resume:

`M1269-M1280 - Installed Sidepanel Manual Verification + Final Demo Polish`
