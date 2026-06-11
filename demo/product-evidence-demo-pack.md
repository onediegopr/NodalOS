# ONE BRAIN Product Evidence Demo Pack

Este paquete permite correr y revisar una demo local estable de evidencia de producto sin depender de sitios externos vivos.

## Que contiene

- Samples versionados: `samples/product-evidence/`
- Documentacion de demo: `docs/demo/`
- Script canonico: `tools/scripts/run-demo-product-evidence.ps1`
- Recipe canonica: `tools/recipes/demo-product-evidence-report.json`
- Version del paquete: `demo/VERSION.md`

## Como correr

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1
```

El script ejecuta la recipe demo y valida el exit code. Al terminar imprime:

```text
LATEST_DEMO_MARKDOWN=<ruta>
```

Ese es el archivo Markdown que se debe abrir para presentar o revisar la demo.

## Que genera

Los outputs runtime quedan bajo `artifacts/`, ignorado por Git:

- Summary JSON: `artifacts/product-evidence-demo-summary/`
- Markdown: `artifacts/product-evidence-demo-reports/`

## Que muestra

- Evidencia normalizada de producto.
- Campos faltantes explicitos.
- Scoring de calidad de evidencia.
- Decision readiness por producto.
- Reporte Markdown local y auditable.

## Safety guarantees

- No browser.
- No web.
- No clicks.
- No login.
- No cookies.
- No carrito.
- No compra.
- No pago.

## Limitaciones

- Demo local con samples versionados.
- No es extraccion live.
- No garantiza disponibilidad ni contenido de sitios externos.
- No reemplaza revision humana final.
- Raw signals no equivalen a precio visible normalizado.

## Snapshot

- Base commit esperado: `428127a document product evidence demo walkthrough`
- Tests esperados: `169/169 PASS`
- Fecha snapshot: `2026-06-11`
