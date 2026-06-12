# HITO-082+083+084 - HTML Demo Polish + Snapshot Fixture + Release Demo Index

## Alcance

Este hito pule el HTML demo, agrega un snapshot HTML versionado y actualiza los indices de demo/release para declarar HTML como salida formal del paquete mostrable.

No agrega PDF, no abre navegador automaticamente, no usa browser/web/red y no modifica safety.

## Cambios aplicados

- `ProductEvidenceHtmlRenderer` agrega:
  - seccion `What this report shows`;
  - seccion `Safety guarantees`;
  - seccion `Decision readiness`;
  - badges visuales para `excellent`, `partial`, `ready_for_comparison` y `needs_price_verification`;
  - notas visibles sobre demo sin web live y precio faltante.
- Se agrega fixture HTML sanitizado:
  - `samples/product-evidence-html/demo-product-evidence-report.html`
- Se actualizan indices:
  - `demo/README.md`
  - `demo/product-evidence-demo-pack.md`
  - `docs/demo/product-evidence-demo.md`
  - `docs/releases/demo-snapshot-current.md`
  - `docs/handoffs/one-brain-release-demo-handoff.md`

## Fixture HTML versionado

El fixture:

- usa `GENERATED_AT_UTC`;
- no contiene rutas `C:\Users\`;
- no contiene paths runtime bajo `artifacts/`;
- representa los 3 samples:
  - Suministros partial;
  - Sodimac partial;
  - Demo complete;
- contiene 1 producto `ready_for_comparison`;
- contiene 2 productos `needs_price_verification`;
- contiene `Products with price = 1`;
- contiene `Products needing price verification = 2`;
- contiene `Safety clicks total = 0`.

## Safety

- No `browser.open`.
- No web/red.
- No clicks.
- No `safe.click`.
- No login.
- No cookies.
- No carrito.
- No compra.
- No pago.
- No WhatsApp.
- No Selenium, DevTools, JS injection ni OCR.
- No se abre HTML automaticamente.

## Validacion esperada

- Build OK.
- Tests PASS.
- `NEGATIVE_EXIT_CODE=1`.
- Demo script OK.
- Demo Markdown dry-run/run OK.
- HTML dry-run/run OK.
- Demo HTML dry-run/run OK.
- ML readonly/diagnostic OK.
- `artifacts/` ignorado y sin artifacts staged.

## Porcentaje actualizado estimado

- Core tecnico: 91-93%
- Safety: 90-92%
- Cleanup: 88-90%
- Validacion/CI confiable: 90-92%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 89-92%
- Demo/reproducibilidad: 92-94%
- MVP comercial serio: 71-74%
- Producto completo/pro: 49-52%
