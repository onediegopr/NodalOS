# ONE BRAIN Product Evidence Demo

Esta demo muestra un flujo local, estable y reproducible para generar evidencia de producto, resumen con scoring, reporte Markdown humano y export HTML local opcional.

## Que muestra la demo

- Lee evidencia de producto versionada desde `samples/product-evidence/`.
- Construye un summary local con conteos, quality status y readiness.
- Genera un Markdown presentable bajo `artifacts/product-evidence-demo-reports/`.
- Puede generar HTML local presentable bajo `artifacts/product-evidence-demo-html-reports/`.
- Muestra campos faltantes de forma explicita, por ejemplo `missing_price`.
- Diferencia evidencia visible normalizada de `rawSignals`.
- Permite demostrar el pipeline sin depender de sitios externos vivos.

## Que NO muestra la demo

- No es extraccion live.
- No navega web.
- No abre browser.
- No hace scraping agresivo.
- No automatiza compras.
- No acepta cookies.
- No inicia sesion.
- No agrega productos a carrito.
- No procesa pagos.
- No reemplaza una revision humana final.
- No convierte `rawSignals` en precio visible normalizado.

## Requisitos

- Windows.
- Repo `onediegopr/OneBrain`.
- SDK portable de ONE BRAIN:
  - `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe`

## Comando para correr

Desde la raiz del repo:

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1
```

Tambien se puede pasar ruta explicita:

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1 `
  -Root "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo" `
  -Dotnet "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe"
```

## Archivos que usa

- `samples/product-evidence/20260611-demo-complete-fixture.json`
- `samples/product-evidence/20260611-demo-sodimac-partial.json`
- `samples/product-evidence/20260611-demo-suministrosroca-partial.json`

Estos samples estan versionados en el repo y son la fuente estable de la demo.

## Archivos que genera

Runtime outputs ignorados por Git:

- `artifacts/product-evidence-demo-summary/`
- `artifacts/product-evidence-demo-reports/`
- `artifacts/product-evidence-demo-html-reports/`

Estos archivos no se commitean.

## HTML local opcional

El reporte HTML usa los mismos samples y summary que el Markdown. No abre navegador ni usa red.

```powershell
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-html-report.json
```

## Como encontrar el ultimo Markdown

El script imprime:

```text
LATEST_DEMO_MARKDOWN=<ruta absoluta al ultimo .md generado>
```

Tambien se puede listar manualmente:

```powershell
Get-ChildItem artifacts/product-evidence-demo-reports -Filter *.md |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 1
```

## Como interpretar el reporte

### evidenceScore

Puntaje de completitud de evidencia capturada. Evalua lo que existe; no crea datos nuevos.

### evidenceGrade

Lectura humana del score:

- `excellent`
- `good`
- `partial`
- `weak`
- `insufficient`

### decisionReadiness

Indica si la evidencia esta lista para comparacion o si necesita mas verificacion.

- `ready_for_comparison`: evidencia suficiente para comparar en la demo.
- `needs_price_verification`: producto identificado, pero falta precio visible confirmado.
- `needs_more_evidence`: faltan campos importantes.
- `diagnostic_only`: solo sirve como diagnostico.

### missing_price

`missing_price` no es un error tecnico por si mismo. Significa que la evidencia visible/UIA no confirmo un precio.

### ready_for_comparison

El item demo completo usa datos fixture controlados y esta marcado como listo para comparacion. No representa evidencia live.

## Safety guarantees

La demo estable confirma:

- no browser;
- no web;
- no clicks;
- no `safe.click`;
- no login;
- no cookies accepted;
- no carrito;
- no compra;
- no pago;
- no WhatsApp.

## Limitaciones honestas

- La demo usa samples versionados.
- No es extraccion live ni prueba de todos los sitios reales.
- No reemplaza revision humana final.
- `rawSignals` no equivalen a evidencia visible normalizada.
- Si un sitio externo bloquea en corridas live, debe reportarse como `partial` o `diagnostic`, sin bypass.
