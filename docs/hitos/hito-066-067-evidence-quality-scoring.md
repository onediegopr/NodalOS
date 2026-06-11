# HITO-066+067 - Report Quality Gates + Evidence Completeness Scoring

## Alcance

Este hito agrega scoring de completitud y quality gates sobre evidencia de producto ya capturada.

No agrega:

- scraping agresivo;
- navegacion web nueva para scoring;
- clicks;
- login;
- carrito;
- compra;
- pago;
- aceptacion de cookies;
- WhatsApp;
- PDF;
- UI.

El scoring evalua datos existentes en `ProductEvidenceSummary`. No cambia reglas de extraccion, no inventa precio y no convierte `rawSignals` en campos visibles normalizados.

## Implementacion

Helper nuevo:

- `ProductEvidenceQualityScorer`

Modelo nuevo:

- `ProductEvidenceQualityScore`

El scoring se integra en:

- `ProductEvidenceSummaryBuilder`
- `ProductEvidenceSummary`
- `ProductEvidenceMarkdownRenderer`

No se agrega accion nueva. Las acciones existentes quedan:

- `artifact.summarizeProductEvidence`
- `report.writeProductEvidenceMarkdown`

## Campos agregados por item

- `evidenceScore`
- `evidenceGrade`
- `qualityStatus`
- `qualityReasons`
- `missingCriticalFields`
- `decisionReadiness`

## Totales agregados

- `sufficientCount`
- `partialCount`
- `insufficientCount`
- `diagnosticCount`
- `averageEvidenceScore`
- `readyForComparisonCount`
- `needsPriceVerificationCount`

## Reglas de scoring

Suma base:

- productName presente: `+25`
- sourceUrl presente: `+15`
- category presente: `+10`
- price + currency visibles presentes: `+25`
- stock presente: `+10`
- confidence high: `+10`
- confidence medium: `+5`
- safety read-only limpia: `+5`

Penalizaciones:

- `missing_price`: `-15`
- `missing_currency`: `-10`
- diagnostic: `-30`
- blocked/geoloc/cookie/captcha/dynamic hard block: `-20`
- señales cart/buy/payment visibles: `-10`

Piso pragmatico:

- si hay producto identificado con `productName + sourceUrl + category` y confianza `medium/high`, pero falta precio, el score no baja de `50`.
- esto representa evidencia parcial util, no extraccion completa.

Grades:

- `85-100`: `excellent`
- `70-84`: `good`
- `50-69`: `partial`
- `25-49`: `weak`
- `0-24`: `insufficient`

Quality status:

- `sufficient`
- `partial`
- `insufficient`
- `diagnostic`

Decision readiness:

- `ready_for_comparison`
- `needs_price_verification`
- `needs_more_evidence`
- `diagnostic_only`

## Reglas de honestidad

- Si `evidence.price=null`, el summary y Markdown mantienen precio faltante.
- Si Sodimac conserva `rawSignals` con `price=38.18`, eso no mejora `evidenceScore` como precio visible.
- `rawSignals` no son precio visible/UIA normalizado.
- Missing price no es error tecnico; es evidencia incompleta.
- Buy/cart/payment signals son senales de riesgo leidas, no acciones ejecutadas.

## Mercado Libre readonly diagnostic

`mercadolibre-product-readonly` puede devolver login, cookies, challenge, captcha, geoloc, contenido dinamico o pantalla sin producto visible por comportamiento externo del sitio.

Cuando eso ocurre:

- se trata como diagnostic read-only controlado;
- no se aceptan cookies;
- no se hace login;
- no se ejecutan clicks;
- no se agrega al carrito;
- no se compra;
- no se paga;
- no se intenta bypass;
- no se considera extraccion completa de producto.

El diagnostico ML no mejora score, no inventa evidencia y no convierte pantallas de bloqueo en datos de producto.

## Markdown

El Markdown agrega:

- quality metrics en Summary;
- columnas `Score`, `Grade`, `Readiness` en Products;
- notas explicando que missing price es evidencia incompleta;
- notas aclarando que rawSignals no son precio visible normalizado.

## Safety

El scoring:

- no abre browser;
- no lee red;
- no ejecuta click;
- no ejecuta `safe.click`;
- no invoca UIA;
- no escribe fuera de artifacts;
- solo evalua artifacts/summary locales.

## Tests

Tests agregados/actualizados:

- `ProductEvidenceQualityScorerTests`
- `ProductEvidenceSummaryBuilderTests`
- `ProductEvidenceMarkdownRendererTests`

Cobertura:

- producto identificado sin precio queda partial / needs_price_verification.
- evidencia completa high confidence queda ready_for_comparison.
- missing price baja readiness.
- diagnostic baja score y queda diagnostic_only.
- rawSignals price no mejora precio visible.
- safety clean agrega razon positiva.
- cart/buy/payment signals bajan readiness.
- summary totals calculan partial/sufficient/average/readiness.
- Markdown incluye Score/Grade/Readiness.
- Markdown mantiene `price=null` como `—`.

## Validacion final

- Build: OK, 0 errors, 0 warnings.
- Tests: OK, 143/143 PASS.
- `NEGATIVE_EXIT_CODE=1`.
- Producto Suministros Roca: PASS.
- Producto Sodimac: PASS.
- Summary con scoring: PASS.
- Markdown con scoring dry-run: PASS, sin side effects.
- Markdown con scoring run: PASS.
- Mercado Libre product readonly/diagnostic: PASS.
- Product search: PASS.
- `artifacts/` ignorado y sin stagear.

Resultado ML observado:

- ML devolvio pantalla generica `Mercado Libre`, con contenido no garantizado de producto.
- La recipe lo acepto como diagnostic read-only controlado.
- `browser.close` cerro la sesion owned correctamente con timeout ampliado.
- No hubo cookies accepted, login, clicks, carrito, compra ni pago.
