# ONE BRAIN Demo QA Freeze Checklist

Checklist para correr la demo estable sin modificar runtime, samples ni sitios externos durante una presentacion.

## Estado congelado

- Commit base: `5916dec package product evidence demo snapshot`
- Tests esperados: `179/179 PASS`
- Negative exit code esperado: `NEGATIVE_EXIT_CODE=1`
- Script canonico: `tools/scripts/run-demo-product-evidence.ps1`
- Recipe canonica: `tools/recipes/demo-product-evidence-report.json`

## Checklist pre-demo

- Confirmar repo correcto: `onediegopr/OneBrain`.
- Confirmar branch `master` limpio.
- Usar SDK portable versionado en `Herramientas/`.
- Ejecutar build/test si se requiere validacion formal.
- Ejecutar `tools/scripts/run-demo-product-evidence.ps1`.
- Verificar que el script imprima `LATEST_DEMO_MARKDOWN`.
- Abrir manualmente el Markdown indicado, sin que el script abra archivos o navegador.

## Checklist safety

- No browser.
- No web.
- No clicks.
- No cookies.
- No login.
- No carrito.
- No compra.
- No pago.
- No Firefox flaky como regression obligatoria.

## Checklist artifacts

- `artifacts/` debe estar ignorado por Git.
- No debe haber artifacts staged.
- `samples/product-evidence/` debe estar versionado.
- Outputs demo esperados:
  - `artifacts/product-evidence-demo-summary/`
  - `artifacts/product-evidence-demo-reports/`

## Checklist demo output

- Summary table visible.
- Products table visible.
- Score visible.
- Grade visible.
- Readiness visible.
- `ready_for_comparison` visible para el demo fixture completo.
- `missing_price` explicado como evidencia incompleta, no como error tecnico.
- Price faltante renderizado como `-` o placeholder equivalente, sin inventar precio.

## Demo rota si ocurre cualquiera de estos casos

- Build falla.
- Tests fallan.
- Recipe negativa devuelve exit code `0`.
- Script no imprime `LATEST_DEMO_MARKDOWN`.
- Demo intenta browser o web.
- Artifacts quedan staged.
- Faltan safety guarantees en README o report.
- Markdown no muestra Summary o Products.
- Markdown no muestra Score, Grade o Readiness.

## No corregir durante demo

- No tocar live sites.
- No intentar resolver cookies, login, challenges o captchas.
- No modificar samples durante la presentacion.
- No cambiar recipes durante la presentacion.
- No cerrar procesos no-owned.
- No convertir rawSignals en precio visible normalizado.
