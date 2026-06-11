# HITO-060+061 - Evidence JSON Artifacts + Report Export

## Alcance

Este hito persiste evidencia normalizada de producto como artefactos JSON locales y auditables.

No agrega:

- PDF.
- UI.
- scraping agresivo.
- interacciones nuevas con web externa.
- clicks.
- login, carrito, compra o pago.

## Accion nueva

Nombre final:

- `artifact.writeProductEvidence`

La accion consume `productEvidence.json`, producido previamente por `extract.productEvidence`, y escribe un JSON local bajo:

- `artifacts/product-evidence/`

La carpeta `artifacts/` ya esta ignorada por Git. Los artefactos generados no se commitean.

## Dry-run

El dry-run actual del CLI no ejecuta steps de recipe; solo valida/describe el plan. Por lo tanto, `artifact.writeProductEvidence` no escribe archivos durante dry-run.

## Schema de artifact

Version:

- `product-evidence-artifact/v1`

Campos:

- `schemaVersion`
- `runId`
- `createdAtUtc`
- `recipeId`
- `profileId`
- `sourceUrl`
- `pageTitle`
- `evidence`
- `safety`
- `validation`
- `notes`

`evidence` contiene el `ProductEvidence` completo:

- `sourceUrl`
- `sourceProfileId`
- `pageTitle`
- `productName`
- `brand`
- `sku`
- `category`
- `description`
- `price`
- `currency`
- `availability`
- `stock`
- `seller`
- `contactSignals`
- `whatsappSignals`
- `cartSignals`
- `buySignals`
- `paymentSignals`
- `loginSignals`
- `cookieSignals`
- `geolocSignals`
- `popupSignals`
- `evidenceTextSample`
- `rawSignals`
- `blockedOrMissingFields`
- `extractionConfidence`
- `extractionStatus`
- `extractionNotes`

`safety`:

- `clicks`: 0
- `cookiesAccepted`: 0
- `loginSignals`
- `cartSignals`
- `buySignals`
- `paymentSignals`
- `whatsappSignals`

`validation`:

- `success`
- `status`
- `confidence`
- `blockedOrMissingFields`

## Path safety

El writer:

- crea la carpeta si no existe;
- sanea `recipeId`, `profileId` y `runId`;
- escribe solo bajo `artifacts/product-evidence`;
- verifica que el path final no escape de esa raiz.

## Recipes actualizadas

- `tools/recipes/suministrosroca-product-readonly-report.json`
- `tools/recipes/sodimac-product-readonly-report.json`

Ambas agregan:

1. `extract.productEvidence`
2. `artifact.writeProductEvidence`
3. reporte con `artifact={{productArtifact.relativePath}}`

## Suministros Roca

Resultado esperado de artifact:

- `evidence.productName`: `Placa Marmol Blanco Firenze`
- `evidence.price`: `null`
- `evidence.currency`: `null`
- `evidence.stock`: `null`
- `validation.status`: `missing_price`
- `validation.confidence`: `medium`
- `validation.blockedOrMissingFields`: incluye `missing_price`, `missing_currency`, `missing_stock`

## Sodimac

Resultado esperado de artifact:

- `evidence.productName`: producto Sodimac leido por UIA.
- `evidence.price`: `null`
- `evidence.currency`: `null`
- `evidence.stock`: `null`
- `evidence.rawSignals`: incluye `Product`, `sku=9065911`, `price=38.18`, `priceCurrency=USD`.
- `validation.status`: `missing_price`
- `validation.confidence`: `medium`
- `notes`: incluye `raw public HTML signal, not visible UIA price`.

Nota importante: raw signals publicos de HTML no equivalen a precio visible UIA. Si `browser.read` no confirma precio/moneda/stock visibles, el artifact conserva `evidence.price=null`, `evidence.currency=null` y `evidence.stock=null`.

## Safety

Confirmado por diseno:

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
- 0 WhatsApp abierto.

La unica escritura nueva es local, bajo `artifacts/product-evidence/`.

## Tests agregados

- serializa `ProductEvidence` completo.
- conserva `price/currency=null`.
- incluye `blockedOrMissingFields`.
- incluye safety summary.
- genera nombre/path seguro.
- crea carpeta si no existe.
- no escribe fuera de `artifacts/product-evidence`.
- rawSignals de Sodimac no se convierten en precio visible.

## Validacion

Validacion ejecutada en branch `hito-060-061-evidence-json-artifacts`:

- `dotnet build OneBrain.slnx`: OK, 0 errors, 0 warnings.
- `dotnet test`: OK, 107/107 PASS.
- `exit-code-negative`: `Success=false`, `NEGATIVE_EXIT_CODE=1`.
- `suministrosroca-product-readonly-report` dry-run: OK, sin side effects.
- `suministrosroca-product-readonly-report` run: OK, escribe artifact JSON.
- `sodimac-product-readonly-report` dry-run: OK, sin side effects.
- `sodimac-product-readonly-report` run: OK, escribe artifact JSON.
- `suministrosroca-category-readonly-report`: OK.
- `sodimac-category-readonly-report`: OK.
- `mercadolibre-product-readonly`: OK en reintento limpio; un intento previo cargo pagina generica externa y fallo assert, sin clicks ni bypass.
- `product-search-report`: OK.

No se ejecuta Firefox fixture porque sigue quarantined diagnostic/flaky/autoRegression=false.

## Artifacts generados en validacion

Rutas generadas bajo `artifacts/product-evidence/`:

- `20260611-192523-suministrosroca-product-readonly-report-suministrosroca-uy-product-c31a28727e0c429c967c1b0c536e5587.json`
- `20260611-192540-sodimac-product-readonly-report-sodimac-product-ca6657e6c46e4c1cb77677d513054d75.json`

`artifacts/` esta ignorado por Git. Estos JSON son outputs locales de validacion y no deben commitearse.

## Observacion de procesos

Al cierre se observo una ventana visible de Edge con el producto Sodimac. No se cerro porque no habia handle owned seguro para esa ventana y el proyecto prohibe cierres genericos de procesos o ventanas no-owned.
