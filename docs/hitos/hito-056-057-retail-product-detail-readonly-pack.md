# HITO-056/057 Retail Product Detail Read-Only Pack

## Alcance

Se agrega un pack read-only para product detail sobre dos retailers publicos:

- Suministros Roca Uruguay product detail.
- Sodimac Uruguay product detail.

El patron se mantiene 100% read-only:

- `profile.load`
- `browser.open`
- `delay`
- `browser.read title`
- `browser.read text`
- `browser.close`
- `extract.visiblefields`
- `discover.actionableelements`
- `plan.safenavigation`
- `if exists`
- `assert.contains`
- `note/report`

## URLs usadas

- `https://suministrosroca.uy/producto/placa-marmol-blanco-firenze/`
- `https://www.sodimac.com.uy/sodimac-uy/product/9065911/piso-flotante-simil-madera-6-mm-essen-cafe-claro-mate-interior-296-m2/9065911/`

## Verificacion previa HTTP 200

Se verifico con `curl.exe -I -L --max-redirs 5`:

- Suministros Roca product detail: `HTTP/1.1 200 OK`
- Sodimac product detail: `HTTP/1.1 200 OK`

## Archivos creados/modificados

Creado:

- `tools/profiles/web/suministrosroca-uy-product.json`
- `tools/profiles/web/sodimac-product.json`
- `tools/recipes/suministrosroca-product-readonly-report.json`
- `tools/recipes/sodimac-product-readonly-report.json`
- `docs/hitos/hito-056-057-retail-product-detail-readonly-pack.md`

Modificado:

- `tests/OneBrain.Recipes.Tests/ProfileLoaderTests.cs`

## Asserts

Suministros Roca:

- `if exists` sobre title
- `assert.contains` sobre `{{bt}} {{btext}}` con senal principal `Firenze`
- fallback conceptual documentado: `ROCA`, `Suministros`, `Placa`

Sodimac:

- `if exists` sobre title
- `assert.contains` sobre `{{bt}} {{btext}}` con `Sodimac`
- fallback conceptual documentado: `Essen`

## Riesgos honestos

- El extractor comercial actual es limitado y no garantiza parseo correcto de precio/currency fuera del patron `$ ...`.
- Sodimac puede exponer cookies, geolocacion, antibot o popups solo como texto visible. No se interactua con nada de eso.
- Si Sodimac no expone el precio via UIA/browser.read, el resultado esperado y honesto es `priceCandidate=null`.
- No se hace JS injection, DevTools, Selenium, OCR ni coordenadas inventadas.

## Resultados de validacion

Validacion ejecutada con dotnet portable:

- Build: OK, 0 errors, 0 warnings.
- Tests: OK, 91/91 PASS.
- Exit code negativo: `exit-code-negative` devolvio JSON `Success=false` y proceso exit code 1.
- Suministros product dry-run: PASS, exit code 0.
- Suministros product run: PASS, `Success=true`, 12/12 steps, exit code 0.
- Sodimac product dry-run: PASS, exit code 0.
- Sodimac product run: PASS, `Success=true`, 12/12 steps, exit code 0.
- Retail regressions: Suministros home/category PASS, Sodimac home/category PASS.
- Public web regressions: Wikipedia PASS, eleconomista PASS.
- Controlled safe click: dry-run PASS, run PASS.
- Mercado Libre read-only/preflight: preflight PASS, action policy PASS, product readonly PASS.
- Product search baseline: PASS.
- Browser stability: PASS.

## Confirmacion read-only

- 0 clicks
- 0 cookies accepted
- 0 login
- 0 carrito
- 0 compra
- 0 pago

## Nota sobre Sodimac

Sodimac UIA/browser.read no expuso `priceCandidate`, `currencyCandidate` ni `stockCandidate` durante la validacion, aunque el HTML publico contenia senales estructuradas `Product`, `sku=9065911`, `price=38.18` y `priceCurrency=USD`.

Esto queda documentado como extraccion UIA parcial aceptada, no como product extraction completa.

Tambien se observo una ventana visible de Edge al final con el producto Sodimac. No se cerro porque ya no habia handle owned disponible; se reporta como posible orphan visible de validacion y se evita un cierre generico por seguridad.

## Porcentaje actualizado estimado

- Core tecnico: 85-87%
- Safety: 86-88%
- Cleanup: 86-88%
- Validacion/CI confiable: 78-83%
- Web read-only: 84-87%
- Retail public read-only: 77-80%
- Safe click controlled: 72-76%
- MVP comercial serio: 53-56%
- Producto completo/pro: 31-34%
