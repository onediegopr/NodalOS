# HITO-070+071 - Demo Report Polish + One-Command Demo Runner

## Alcance

Este hito pule el reporte Markdown demo y agrega una forma one-command para correr la demo local estable.

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
- cookies;
- WhatsApp;
- PDF;
- UI.

La demo usa solo samples versionados bajo `samples/` y escribe outputs runtime bajo `artifacts/`.

## Markdown demo

El renderer conserva el modo generico para summaries normales y activa pulido demo cuando detecta artifacts desde:

- `samples/product-evidence/`
- `demo-fixture`

Secciones agregadas para demo:

- `ONE BRAIN - Stable Product Evidence Demo`
- `What this demo shows`
- `Important safety guarantees`
- `Decision readiness`

El reporte mantiene:

- tabla `Summary`;
- tabla `Products`;
- tabla/seccion `Invalid artifacts`;
- precio `null` renderizado como `—`;
- moneda `null` renderizada como `—`;
- `rawSignals` sin promocionarse a precio visible normalizado.

Metricas renombradas:

- `Products missing price` pasa a `Products needing price verification`.

## One-command runner

Script creado:

- `tools/scripts/run-demo-product-evidence.ps1`

Uso:

```powershell
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1
```

Parametros opcionales:

- `-Root`
- `-Dotnet`

Defaults:

- `Root`: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo`
- `Dotnet`: `C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe`

El script:

- valida que el repo sea `onediegopr/OneBrain`;
- usa el SDK portable por defecto;
- ejecuta `tools/recipes/demo-product-evidence-report.json`;
- valida `$LASTEXITCODE`;
- imprime `LATEST_DEMO_MARKDOWN`;
- no abre el Markdown automaticamente;
- no abre browser.

## Recipe demo

Recipe canonica:

- `tools/recipes/demo-product-evidence-report.json`

Metadata agregada:

- `oneCommandDemo: true`
- `sampleInputDir`
- `summaryOutputDir`
- `markdownOutputDir`

El note/report final incluye:

- conteos de artifacts;
- conteos de precio/completitud;
- readiness;
- `summaryPath`;
- `markdownOutputPath`;
- confirmacion local-only.

## Safety

Confirmado para demo:

- 0 browser/web/red;
- 0 clicks;
- 0 cookies accepted;
- 0 login;
- 0 carrito;
- 0 compra;
- 0 pago;
- 0 WhatsApp;
- 0 cierre de procesos.

Las regresiones externas siguen separadas y no forman parte del one-command demo.

## Artifacts

El script y la recipe escriben bajo:

- `artifacts/product-evidence-demo-summary/`
- `artifacts/product-evidence-demo-reports/`

`artifacts/` esta ignorado por Git. Los reports generados no se commitean.

## Tests

Tests agregados/actualizados:

- renderer incluye titulo demo;
- renderer incluye `What this demo shows`;
- renderer incluye safety guarantees;
- renderer incluye `Decision readiness`;
- renderer mantiene precio/moneda null como `—`;
- recipe demo sigue sin browser/input actions;
- script existe;
- script referencia la recipe demo;
- script valida `$LASTEXITCODE`;
- script usa SDK portable por defecto;
- script no usa global `dotnet`;
- script no abre browser/archivo.

## Validacion

Validaciones esperadas:

- `dotnet build OneBrain.slnx`: 0 errors, 0 warnings.
- `dotnet test`: PASS.
- `exit-code-negative`: `Success=false`, exit code `1`.
- `demo-product-evidence-report` dry-run: PASS, 0 sensitive.
- `demo-product-evidence-report` run: PASS.
- `tools/scripts/run-demo-product-evidence.ps1`: PASS.
- `product-evidence-summary-report`: PASS.
- `product-evidence-markdown-report` dry-run/run: PASS.
- `mercadolibre-product-readonly`: PASS diagnostic/read-only si el sitio bloquea.
- `product-search-report`: PASS o diagnostic externo controlado.

## Porcentaje actualizado estimado

- Core tecnico: `89-91%`
- Safety: `89-91%`
- Cleanup: `88-90%`
- Validacion/CI confiable: `87-89%`
- Web read-only: `87-89%`
- Retail public read-only: `82-84%`
- Evidence/reporting: `82-85%`
- Demo/reproducibilidad: `78-82%`
- MVP comercial serio: `63-66%`
- Producto completo/pro: `41-44%`
