# HITO-079+080+081 - HTML Report Export + Styled Demo Report + Golden Snapshot Tests

## Alcance

Este hito agrega export HTML local para `ProductEvidenceSummary`, integra un reporte HTML demo desde samples versionados y agrega tests semanticos tipo golden/snapshot para evitar regresiones en secciones clave.

No agrega PDF, UI, browser auto-open, navegacion web ni scraping live.

## Archivos creados/modificados

- `src/OneBrain.Core/Extraction/ProductEvidenceHtmlReport.cs`
- `src/OneBrain.Core/Extraction/ProductEvidenceHtmlRenderer.cs`
- `src/OneBrain.Core/Extraction/ProductEvidenceHtmlWriter.cs`
- `src/OneBrain.Cli/Recipes/RecipeRunner.cs`
- `tests/OneBrain.Recipes.Tests/ProductEvidenceHtmlRendererTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductEvidenceHtmlWriterTests.cs`
- `tests/OneBrain.Recipes.Tests/ProductEvidenceDemoGoldenTests.cs`
- `tools/recipes/product-evidence-html-report.json`
- `tools/recipes/demo-product-evidence-html-report.json`
- `demo/README.md`
- `docs/demo/product-evidence-demo.md`
- `docs/handoffs/one-brain-release-demo-handoff.md`

## Accion final

Accion nueva:

- `report.writeProductEvidenceHtml`

Entrada:

- `summaryFrom`: prefijo de un `ProductEvidenceSummary` ya generado por `artifact.summarizeProductEvidence`.
- `outputDir`: carpeta de salida bajo `artifacts/`.

Salida:

- `*.success`
- `*.path`
- `*.relativePath`
- `*.error`

La accion no abre browser, no lee red, no hace clicks y no interactua con paginas. Solo consume variables ya capturadas y escribe HTML local.

## Output HTML

Output runtime general:

- `artifacts/product-evidence-html-reports/`

Output runtime demo:

- `artifacts/product-evidence-demo-html-reports/`

Los artifacts runtime siguen ignorados por Git y no se commitean.

## Estructura HTML

El HTML incluye:

- `<!doctype html>`
- `<title>ONE BRAIN - Product Evidence Report</title>`
- header `ONE BRAIN - Product Evidence Report`
- timestamp generado
- summary cards:
  - Source artifacts
  - Valid artifacts
  - Invalid artifacts
  - Products with price
  - Products needing price verification
  - Average evidence score
  - Ready for comparison
  - Safety clicks total
- products table:
  - Product
  - Source
  - Price
  - Currency
  - Status
  - Confidence
  - Score
  - Grade
  - Readiness
  - Missing fields
- notes
- invalid artifacts

Reglas:

- `price=null` renderiza `-` visualmente como em dash.
- `currency=null` renderiza `-` visualmente como em dash.
- `rawSignals` no se convierten en precio visible normalizado.
- HTML se escapa con encoding.
- CSS es embebido/local.
- No hay CSS externo, JS, CDN ni links externos requeridos.

## Recipes

General:

```powershell
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/product-evidence-html-report.json
```

Demo:

```powershell
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-html-report.json
```

Ambas recipes son locales y no contienen acciones browser/web/click.

## Golden / snapshot testing strategy

Se usa snapshot semantico, no comparacion exacta fragil de archivo completo.

Los tests:

- renderizan summaries deterministas desde samples versionados;
- verifican doctype, title/header, summary, products, notes e invalid artifacts;
- verifican columnas `Score`, `Grade`, `Readiness`;
- verifican `ready_for_comparison`, `missing_price` y `Safety clicks total`;
- verifican que el fixture completo renderiza `199.00 USD`;
- verifican que productos parciales renderizan precio/currency como em dash;
- verifican escaping HTML;
- verifican ausencia de CSS/JS/CDN externo;
- verifican que `rawSignals price=38.18` no aparece como precio normalizado.

## Safety

Confirmaciones de diseno:

- no `browser.open`;
- no web/red;
- no clicks;
- no `safe.click`;
- no invoke;
- no type;
- no submit;
- no login;
- no cookies;
- no carrito;
- no compra;
- no pago;
- no WhatsApp;
- no cierre de procesos;
- no Selenium, DevTools, JS injection ni OCR;
- no se abre HTML automaticamente.

## Validacion esperada

- Build OK.
- Tests PASS.
- `NEGATIVE_EXIT_CODE=1`.
- Demo script OK.
- Demo dry-run/run OK.
- HTML dry-run/run OK.
- Demo HTML dry-run/run OK.
- Summary/Markdown/ML regressions OK.
- `artifacts/` ignorado y sin artifacts staged.

## Limitaciones

- No PDF.
- No browser auto-open.
- No UI.
- No live scraping.
- No garantiza precio visible si la evidencia normalizada no lo contiene.
- No intenta resolver bloqueos externos.

## Porcentaje actualizado estimado

- Core tecnico: 91-93%
- Safety: 90-92%
- Cleanup: 88-90%
- Validacion/CI confiable: 90-92%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 88-91%
- Demo/reproducibilidad: 91-93%
- MVP comercial serio: 70-73%
- Producto completo/pro: 48-51%
