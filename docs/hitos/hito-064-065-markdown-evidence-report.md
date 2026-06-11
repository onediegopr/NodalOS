# HITO-064+065 - Markdown Evidence Report + Human-Readable Export

## Alcance

Este hito agrega export humano en Markdown a partir de `ProductEvidenceSummary`.

No agrega:

- PDF.
- UI.
- navegacion web nueva.
- scraping adicional.
- clicks.
- login, carrito, compra o pago.
- aceptacion de cookies.

El input es un summary local generado desde artifacts ya existentes.

## Accion nueva

Nombre final:

- `report.writeProductEvidenceMarkdown`

La accion:

- consume `productEvidenceSummary.json` desde variables de recipe;
- renderiza Markdown local;
- escribe bajo `artifacts/product-evidence-reports/`;
- no abre browser;
- no usa red;
- no ejecuta acciones UI.

## Estructura del Markdown

Titulo:

- `# ONE BRAIN - Product Evidence Summary`

Secciones:

- `Generated`
- `## Summary`
- `## Products`
- `## Notes`
- `## Invalid artifacts`

La tabla `Summary` contiene:

- Source artifacts.
- Valid artifacts.
- Invalid artifacts.
- Products with price.
- Products missing price.
- Safety clicks total.
- Safety payment signals total.

La tabla `Products` contiene:

- Product.
- Source.
- Price.
- Currency.
- Status.
- Confidence.
- Missing fields.

## Regla de precio

El renderer no inventa precio.

Si `price=null`, renderiza:

- `—`

Si `currency=null`, renderiza:

- `—`

Si rawSignals contiene algo como `price=38.18` pero el summary tiene `price=null`, el reporte mantiene `Price=—` y agrega nota indicando que rawSignals no son precio visible normalizado.

## Path safety

El writer:

- escribe solo bajo `artifacts/product-evidence-reports/`;
- crea la carpeta si no existe;
- verifica que el output no escape de esa raiz;
- si el nombre de salida ya existe, usa un sufijo incremental para no sobrescribir.

## Recipe

Recipe agregada:

- `tools/recipes/product-evidence-markdown-report.json`

Pasos:

1. `artifact.summarizeProductEvidence`
2. `report.writeProductEvidenceMarkdown`
3. `note`

No contiene:

- `browser.open`
- `click`
- `safe.click`
- `invoke`
- `type`
- `submit`

## Artifacts

Los Markdown generados quedan bajo:

- `artifacts/product-evidence-reports/`

`artifacts/` esta ignorado por Git. Estos outputs locales no deben commitearse.

`sourceArtifactCount` resume los artifacts locales presentes al momento de la corrida. Puede incluir artifacts acumulados de corridas previas dentro de `artifacts/product-evidence/`; no implica navegacion nueva ni scraping adicional.

## Tests agregados

- renderer genera titulo principal.
- renderer incluye tabla Summary.
- renderer incluye tabla Products.
- `price=null` renderiza `—`.
- `currency=null` renderiza `—`.
- rawSignals price no aparece como precio normalizado.
- missing fields aparecen en tabla.
- safety clicks total aparece en 0.
- escapa pipes y newlines en celdas Markdown.
- writer crea carpeta si no existe.
- writer escribe solo bajo `artifacts/product-evidence-reports/`.
- writer no sobreescribe report existente.
- summary vacio genera reporte diagnostico controlado.

## Safety

Confirmado por diseno:

- 0 browser nuevo en Markdown report.
- 0 clicks.
- 0 `safe.click`.
- 0 invoke.
- 0 type.
- 0 submit.
- 0 cookies accepted.
- 0 login.
- 0 carrito.
- 0 compra.
- 0 pago.

No se ejecuta Firefox fixture porque sigue quarantined diagnostic/flaky/autoRegression=false.

## Validacion final

- Branch: `hito-064-065-markdown-evidence-report`
- Build: OK, 0 errors, 0 warnings.
- Tests: OK, 132/132 PASS.
- `exit-code-negative`: `Success=false`, `NEGATIVE_EXIT_CODE=1`.
- `suministrosroca-product-readonly-report`: PASS.
- `sodimac-product-readonly-report`: PASS.
- `product-evidence-summary-report`: PASS.
- `product-evidence-markdown-report` dry-run: PASS, sin side effects.
- `product-evidence-markdown-report` run: PASS.
- `mercadolibre-product-readonly`: PASS.
- `product-search-report`: PASS.

Markdown generado durante validacion:

- `artifacts/product-evidence-reports/20260611-203523-product-evidence-report.md`

Conteos observados en el reporte generado:

- `sourceArtifactCount=16`
- `validArtifactCount=16`
- `invalidArtifactCount=0`
- `productsWithPrice=0`
- `productsMissingPrice=16`
- `safetyClicksTotal=0`

`artifacts/` aparece como ignorado por Git y no se stagearon artifacts generados.
