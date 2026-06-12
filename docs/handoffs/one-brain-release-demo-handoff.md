# ONE BRAIN Release Demo Handoff

Documento maestro para retomar desarrollo, presentar el estado o iniciar un nuevo chat sin perder contexto.

## 1. Identidad del proyecto

- Proyecto: ONE BRAIN
- Ruta local: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`
- Repo esperado: `onediegopr/OneBrain`
- Branch base: `master`
- Ultimo commit confirmado: `02d90b0 freeze demo qa and public storyline`
- SDK portable obligatorio: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe`
- No usar global dotnet.

## 2. Estado Git confirmado

Ultimos commits relevantes:

- `02d90b0 freeze demo qa and public storyline`
- `5916dec package product evidence demo snapshot`
- `428127a document product evidence demo walkthrough`
- `31e3dcd add one-command product evidence demo runner`
- `ea153be add stable product evidence demo pack`
- `a6dca67 score product evidence quality`
- `ae15513 write product evidence markdown reports`
- `5cc7538 summarize product evidence artifacts`
- `827dce7 write product evidence json artifacts`
- `c53b154 add product evidence normalization`
- `bc82049 add retail product detail readonly profiles`
- `7b4f325 harden cli exit codes for failed recipes`

## 3. Validacion actual esperada

- Build OK: `0 errors`, `0 warnings`
- Tests: `189/189 PASS`
- Validacion del handoff actual: `200/200 PASS`
- Negative recipe: `NEGATIVE_EXIT_CODE=1`
- Demo script: OK
- Demo dry-run/run: OK
- `artifacts/` ignorado por Git
- `git status --short`: limpio

## 4. Comandos canonicos de validacion

```powershell
$ErrorActionPreference = "Stop"

$root = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo"
$dotnet = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe"

Set-Location $root

git remote -v
git branch --show-current
git status --short
git log --oneline -12

& $dotnet build OneBrain.slnx
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

& $dotnet test
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/exit-code-negative.json
$negativeExit = $LASTEXITCODE
Write-Host "NEGATIVE_EXIT_CODE=$negativeExit"
if ($negativeExit -eq 0) { throw "Failed recipe returned exit code 0" }

powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1 -Root $root -Dotnet $dotnet
if ($LASTEXITCODE -ne 0) { throw "Demo script failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe dry-run tools/recipes/demo-product-evidence-report.json
if ($LASTEXITCODE -ne 0) { throw "Demo dry-run failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-report.json
if ($LASTEXITCODE -ne 0) { throw "Demo run failed" }

git status --short
git status --ignored --short artifacts
```

## 5. Capacidades actuales

### Runtime local Windows/C#

- Runtime local Windows/C#.
- UI Automation first.
- BrowserSession owned.
- `browser.open`, `browser.read`, `browser.close`.
- Recipe JSON runner.
- Variables/templates.
- Outputs/assertions.
- Dry-run.
- Failure artifacts.

### Safety y planificacion

- `safe.click` controlado.
- Approval manifest.
- Action discovery.
- Risk policy classification.
- Safe navigation planning.
- `app.close` v1.

### Web y retail read-only

- Retail read-only home/category/product.
- Mercado Libre read-only/diagnostic.
- Public web read-only probado.

### Evidence/reporting

- ProductEvidence normalization.
- JSON artifacts.
- Summary artifacts.
- Markdown report.
- HTML report export local.
- Quality score / grade / readiness.

### Demo

- Stable demo pack.
- One-command demo runner.
- Demo README/walkthrough.
- Demo packaging/release snapshot.
- QA freeze/public storyline.

## 6. Safety vigente

- No commercial clicks.
- No login.
- No cookies accepted.
- No carrito.
- No compra.
- No pago.
- No checkout.
- No WhatsApp.
- No submit.
- No JS injection.
- No DevTools.
- No Selenium.
- No OCR salvo ultimo recurso.
- No coordenadas inventadas.
- No cerrar procesos no-owned.
- No global taskkill.
- Firefox fixture quarantined, no regression obligatoria.

## 7. Demo estable

- Script: `tools/scripts/run-demo-product-evidence.ps1`
- Recipe: `tools/recipes/demo-product-evidence-report.json`
- Samples: `samples/product-evidence/`
- Docs:
  - `demo/`
  - `docs/demo/`
  - `docs/releases/demo-snapshot-current.md`
- Outputs runtime:
  - `artifacts/product-evidence-demo-summary/`
  - `artifacts/product-evidence-demo-reports/`
  - `artifacts/product-evidence-demo-html-reports/`
- El script imprime `LATEST_DEMO_MARKDOWN`.

### Que muestra

- ProductEvidence.
- Summary.
- Markdown.
- HTML local opcional.
- Score/grade/readiness.
- `partial` vs `ready_for_comparison`.

### Que no muestra

- Extraccion live garantizada.
- Compra automatica.
- Evitar todos los bloqueos externos.
- Aceptacion de cookies.
- Checkout/pago.

## 8. Estado Mercado Libre

- Mercado Libre puede devolver login/cookies/challenge/no-product.
- Eso se trata como diagnostic read-only controlado.
- No se intenta resolver challenge.
- No se acepta cookie.
- No se hace login.
- No se clickea.
- No se inventa evidencia.
- La recipe `mercadolibre-product-readonly.json` fue reclasificada como external-fragile/read-only/diagnosticAllowed.

## 9. Porcentaje actualizado

- Core tecnico: 90-92%
- Safety: 90-92%
- Cleanup: 88-90%
- Validacion/CI confiable: 89-91%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 85-88%
- Demo/reproducibilidad: 90-92%
- MVP comercial serio: 68-71%
- Producto completo/pro: 46-49%

## 10. Que sigue recomendado

- HITO-079+080+081 - HTML Report Export + Styled Demo Report + Golden Snapshot Tests.
- HITO-082+083 - Demo Report Snapshot Expansion + Golden Files.
- HITO-083+084 - Live Evidence Robustness / External Block Classification.
- HITO-085+086 - Approval UX / Remote Review Concept.
- HITO-087+088 - Local/Private Enterprise Mode Notes.
- HITO-089+090 - Commercial MVP Landing Narrative.

## 11. Prompt de arranque para nuevo chat

```text
PROYECTO: ONE BRAIN
RUTA: C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo
REPO ESPERADO: onediegopr/OneBrain

Continuar desde master limpio.

Estado base:
- Ultimo commit: 02d90b0 freeze demo qa and public storyline
- Build esperado: 0 errors, 0 warnings
- Tests esperados: 189/189 PASS
- NEGATIVE_EXIT_CODE=1
- Demo runner: tools/scripts/run-demo-product-evidence.ps1
- Demo recipe: tools/recipes/demo-product-evidence-report.json
- Runtime artifacts bajo artifacts/ ignorado por Git
- Firefox fixture quarantined diagnostic/flaky/autoRegression=false

Safety vigente:
- No commercial clicks
- No login
- No cookies accepted
- No carrito
- No compra
- No pago
- No checkout
- No WhatsApp
- No JS injection
- No DevTools
- No Selenium
- No OCR salvo ultimo recurso
- No coordenadas inventadas
- No cerrar procesos no-owned
- No global taskkill

Proximo hito recomendado:
HITO-082+083 - Demo Report Snapshot Expansion + Golden Files.

Antes de tocar archivos:
- verificar repo remoto onediegopr/OneBrain;
- verificar branch master limpio;
- verificar ultimo commit 02d90b0;
- usar SDK portable, no global dotnet.
```

## Claims prohibidos como promesas positivas

Estas frases pueden aparecer solo como limites, anti-claims o cosas que no se deben prometer:

- compra automaticamente
- evita todos los bloqueos
- extrae todos los precios
- 100% autonomo
- bypass
- garantiza precio
