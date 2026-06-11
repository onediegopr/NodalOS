# ONE BRAIN Demo Snapshot

Snapshot documental del estado actual de la demo estable de evidencia de producto.

## Base

- Fecha: 2026-06-11
- Ultimo commit base: `428127a document product evidence demo walkthrough`
- Tests esperados: `169/169 PASS`
- Demo script: `tools/scripts/run-demo-product-evidence.ps1`
- Demo recipe: `tools/recipes/demo-product-evidence-report.json`
- Samples versionados: `samples/product-evidence/`

## Hitos incluidos

- HITO-051+052: retail publico home read-only.
- HITO-053+054: retail categoria/producto read-only.
- HITO-055: exit code hardening y quarantine Firefox fixture.
- HITO-056+057: product detail read-only.
- HITO-058+059: normalizacion de evidencia de producto.
- HITO-060+061: artifacts JSON de evidencia.
- HITO-062+063: summary/comparacion multi-producto.
- HITO-064+065: Markdown report humano.
- HITO-066+067: quality gates y evidence scoring.
- HITO-068+069: demo pack con samples estables.
- HITO-070+071: one-command demo runner.
- HITO-072+073: README y walkthrough de demo.

## Capacidades actuales

- Retail/product read-only.
- Normalizacion de evidencia.
- JSON artifacts locales.
- Summary multi-producto.
- Markdown report humano.
- Evidence score, grade y decision readiness.
- Samples estables versionados.
- Demo local one-command.
- README y walkthrough para demo comercial/tecnica.

## Safety guarantees

- No clicks comerciales.
- No login.
- No cookies accepted.
- No carrito.
- No compra.
- No pago.
- No checkout.
- No WhatsApp.
- No Selenium, DevTools, JS injection u OCR.
- Firefox fixture queda quarantined como diagnostic/flaky y no es regression obligatoria.

## Que no hace todavia

- No compra.
- No checkout.
- No login.
- No acepta cookies.
- No genera PDF.
- No tiene UI final.
- No garantiza extraccion live en sitios externos.
- No intenta resolver bloqueos externos.

## Validacion minima

```powershell
$root = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo"
$dotnet = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe"
Set-Location $root
& $dotnet build OneBrain.slnx
& $dotnet test
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/exit-code-negative.json
```

La recipe negativa debe devolver `Success=false` y exit code distinto de cero.

## Como correr la demo

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1
```

El script imprime `LATEST_DEMO_MARKDOWN` con el Markdown generado bajo `artifacts/product-evidence-demo-reports/`.

## Estado porcentual estimado

- Core tecnico: 89-91%
- Safety: 89-91%
- Cleanup: 88-90%
- Validacion/CI confiable: 88-90%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 84-87%
- Demo/reproducibilidad: 85-88%
- MVP comercial serio: 65-68%
- Producto completo/pro: 43-46%

## Claims y limites que no se deben vender como promesas

Estas frases pueden aparecer solo como claims prohibidos o limites, no como capacidades positivas:

- compra automaticamente
- evita todos los bloqueos
- extrae todos los precios
- 100% autonomo sin supervision
- bypass
- garantiza precio
