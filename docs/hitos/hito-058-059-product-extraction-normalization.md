# HITO-058+059 - Product Extraction Schema + Read-Only Evidence Normalization

## Alcance

Este hito agrega una capa de normalizacion de evidencia de producto para reportes read-only. No agrega scraping agresivo ni interaccion con sitios externos.

La fuente autorizada es:

- titulo leido por `browser.read`;
- texto visible leido por `browser.read`;
- variables ya producidas por `extract.visibleFields`, `discover.actionableElements` y `plan.safeNavigation`;
- raw signals publicos documentados cuando ya fueron verificados fuera de una accion interactiva.

## Decision tecnica

Se eligio una accion read-only nueva:

- `extract.productEvidence`

La accion vive en el runner solo como adaptador de recipe. La logica real es pura y esta en:

- `ProductEvidence`
- `ProductEvidenceInput`
- `ProductEvidenceExtractor`

Motivo: mantiene la normalizacion en `OneBrain.Core`, testeable sin UI, y evita tocar `BrowserSession`, safety, clicks o navegacion.

## Schema normalizado

Campos:

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

Estados:

- `complete`
- `missing_price`
- `missing_stock`
- `diagnostic`

Confianza:

- `high`: hay `productName` y precio/moneda visible o SKU visible.
- `medium`: hay `productName` o categoria, pero faltan precio o stock.
- `low`: solo hay senales genericas.
- `diagnostic`: no hay producto claro o hay bloqueo/dinamica sin producto util.

## Integracion de recipes

Actualizadas:

- `tools/recipes/suministrosroca-product-readonly-report.json`
- `tools/recipes/sodimac-product-readonly-report.json`

Ambas agregan `extract.productEvidence` despues de `plan.safeNavigation` y antes del reporte final.

No se agregaron acciones interactivas.

## Suministros Roca

URL:

- `https://suministrosroca.uy/producto/placa-marmol-blanco-firenze/`

Resultado esperado:

- `productName`: `Placa Marmol Blanco Firenze`
- `category`: `placa/revestimiento`
- `price`: `null` si no aparece en UIA/texto visible.
- `currency`: `null` si no aparece en UIA/texto visible.
- `stock`: `null` si no aparece en UIA/texto visible.
- `extractionStatus`: `missing_price`
- `extractionConfidence`: `medium`

## Sodimac

URL:

- `https://www.sodimac.com.uy/sodimac-uy/product/9065911/piso-flotante-simil-madera-6-mm-essen-cafe-claro-mate-interior-296-m2/9065911/`

Raw signals publicos documentados:

- `Product`
- `sku=9065911`
- `price=38.18`
- `priceCurrency=USD`

Nota importante: esos raw signals quedan registrados como contexto documentado. Si `browser.read`/UIA no expone precio, moneda o stock, `price`, `currency` y `stock` permanecen `null`. Este hito no convierte raw signals estaticos en precio normalizado visible.

Resultado esperado:

- `productName`: titulo del producto Sodimac.
- `category`: `pisos/revestimientos`
- `price`: `null` si no aparece en UIA/texto visible.
- `currency`: `null` si no aparece en UIA/texto visible.
- `stock`: `null` si no aparece en UIA/texto visible.
- `rawSignals`: contiene las senales publicas documentadas.
- `extractionStatus`: `missing_price`
- `extractionConfidence`: `medium`

## Safety

Confirmado por diseno:

- 0 clicks.
- 0 `safe.click`.
- 0 `invoke`.
- 0 `type`.
- 0 `submit`.
- 0 cookies accepted.
- 0 login.
- 0 carrito.
- 0 compra.
- 0 pago.
- 0 WhatsApp abierto.

`extract.productEvidence` solo lee variables locales ya capturadas y produce JSON local.

## Tests

Agregados tests unitarios puros para:

- extraer `productName` desde titulo de Suministros.
- extraer `productName`/categoria desde Sodimac.
- detectar `USD 38.18` cuando aparece en texto visible.
- no inventar precio cuando no hay patron.
- marcar `missing_price`.
- detectar senales de carrito/comprar/pago/login/WhatsApp como senales, no acciones.
- calcular confidence `medium` sin precio.
- calcular `diagnostic` ante bloqueo/cookie/geoloc sin producto claro.

## Validacion

Ejecutado en branch `hito-058-059-product-extraction-normalization`:

- build: OK, 0 warnings, 0 errors.
- tests: OK, 99/99 PASS.
- `exit-code-negative`: `Success=false`, `NEGATIVE_EXIT_CODE=1`.
- Suministros product readonly: PASS, `Success=true`, 13/13.
- Sodimac product readonly: PASS, `Success=true`, 13/13.
- Suministros category readonly: PASS.
- Sodimac category readonly: PASS.
- Mercado Libre product readonly: PASS, read-only.
- product search: PASS.

No se ejecuta Firefox fixture porque sigue quarantined diagnostic/flaky/autoRegression=false.

Inspeccion no destructiva de procesos: se observo una ventana visible de Edge con el producto Sodimac. No se cerro porque desde fuera del runner no habia garantia de handle owned. Se reporta como observacion de cleanup/validacion y se evita cierre generico por seguridad.
