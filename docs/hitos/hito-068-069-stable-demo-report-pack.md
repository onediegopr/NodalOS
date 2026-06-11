# HITO-068+069 - Stable Demo Report Pack + Sample Artifacts

## Alcance

Este hito crea un paquete demo estable y reproducible basado en samples locales versionados.

No agrega:

- scraping;
- navegacion web;
- browser automation;
- clicks;
- `safe.click`;
- login;
- carrito;
- compra;
- pago;
- aceptacion de cookies;
- WhatsApp;
- PDF;
- UI.

El objetivo es poder demostrar el pipeline de evidence summary, scoring y Markdown sin depender de sitios externos vivos.

## Samples versionados

Los artifacts runtime siguen en `artifacts/` y no se versionan. Para demo se agregan fixtures controlados en:

- `samples/product-evidence/`
- `samples/product-evidence-summary/`
- `samples/product-evidence-reports/`

Samples creados:

- `samples/product-evidence/20260611-demo-suministrosroca-partial.json`
- `samples/product-evidence/20260611-demo-sodimac-partial.json`
- `samples/product-evidence/20260611-demo-complete-fixture.json`

## Casos cubiertos

### Suministros Roca partial

- Producto: `Placa Marmol Blanco Firenze`
- Precio: `null`
- Moneda: `null`
- Stock: `null`
- Estado: `missing_price`
- Confianza: `medium`
- Score esperado: `50`
- Grade esperado: `partial`
- Readiness esperado: `needs_price_verification`

### Sodimac partial

- Producto: `Piso flotante simil madera 6 mm Essen cafe claro mate interior 2.96 m2`
- Precio visible normalizado: `null`
- Moneda visible normalizada: `null`
- Stock: `null`
- `rawSignals` contiene `price=38.18` y `priceCurrency=USD`
- Estado: `missing_price`
- Confianza: `medium`
- Score esperado: `50`
- Grade esperado: `partial`
- Readiness esperado: `needs_price_verification`

El precio en `rawSignals` no se normaliza como precio visible. El sample preserva la regla de honestidad del pipeline: no inventar precio, moneda ni stock si la evidencia visible no los confirma.

### Demo complete fixture

- Producto: `Demo Fixture Product`
- `sourceProfileId`: `demo-fixture`
- `sourceUrl`: `https://example.invalid/demo-product`
- Precio: `199.00`
- Moneda: `USD`
- Stock: `in_stock`
- Estado: `complete`
- Confianza: `high`
- Score esperado: `100`
- Grade esperado: `excellent`
- Readiness esperado: `ready_for_comparison`

Este sample esta marcado como fixture demo. No representa evidencia web viva.

## Recipe demo

Recipe creada:

- `tools/recipes/demo-product-evidence-report.json`

Flujo:

1. `artifact.summarizeProductEvidence`
   - `inputDir`: `samples/product-evidence`
   - `outputDir`: `artifacts/product-evidence-demo-summary`
   - `saveAs`: `demoSummary`
2. `report.writeProductEvidenceMarkdown`
   - `summaryFrom`: `demoSummary`
   - `outputDir`: `artifacts/product-evidence-demo-reports`
   - `saveAs`: `demoMarkdown`
3. `note/report`
   - reporta conteos, scoring y ruta Markdown.

La recipe no contiene acciones browser, input, click, invoke, red ni web.

## Conteos esperados

- `sourceArtifactCount`: `3`
- `validArtifactCount`: `3`
- `invalidArtifactCount`: `0`
- `productsWithPrice`: `1`
- `productsMissingPrice`: `2`
- `partialCount`: `2`
- `sufficientCount`: `1`
- `readyForComparisonCount`: `1`
- `averageEvidenceScore`: `66.67`
- `safetyClicksTotal`: `0`

## Outputs runtime

La corrida real escribe outputs ignorados por Git bajo:

- `artifacts/product-evidence-demo-summary/`
- `artifacts/product-evidence-demo-reports/`

Los outputs generados son artifacts runtime y no se commitean.

## Cambios de runtime acotados

Se permite que el summary lea artifacts desde:

- `artifacts/product-evidence/`
- `samples/product-evidence/`

Se permite que los writers escriban bajo subcarpetas controladas de:

- `artifacts/`

Esto habilita demo outputs separados sin debilitar la regla de no escribir fuera de artifacts.

## Safety

Confirmaciones:

- 0 clicks;
- 0 cookies accepted;
- 0 login;
- 0 carrito;
- 0 compra;
- 0 pago;
- 0 WhatsApp;
- 0 browser/web/red en la recipe demo.

## Validacion

Validaciones esperadas:

- `dotnet build OneBrain.slnx`: 0 errors, 0 warnings.
- `dotnet test`: PASS.
- `exit-code-negative`: `Success=false`, exit code `1`.
- `demo-product-evidence-report` dry-run: PASS, sin side effects.
- `demo-product-evidence-report` run: PASS, escribe Markdown en `artifacts/`.
- `product-evidence-summary-report`: PASS.
- `product-evidence-markdown-report` dry-run/run: PASS.
- `mercadolibre-product-readonly`: PASS diagnostic/read-only si el sitio bloquea.
- `product-search-report`: PASS o diagnostic externo controlado.

## Pendientes

- PDF export.
- UI de visualizacion.
- Firma/hash de reports demo.
- Pack de demo para multiples dominios controlados adicionales.

## Porcentaje actualizado estimado

- Core tecnico: `88-90%`
- Safety: `88-90%`
- Cleanup: `88-89%`
- Validacion/CI confiable: `86-88%`
- Web read-only: `87-89%`
- Retail public read-only: `82-84%`
- Evidence/reporting: `80-83%`
- Demo/reproducibilidad: `70-75%`
- MVP comercial serio: `61-64%`
- Producto completo/pro: `39-42%`
