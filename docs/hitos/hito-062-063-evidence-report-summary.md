# HITO-062+063 - Evidence Report Summary + Multi-Product Comparison

## Alcance

Este hito agrega una capa local de resumen y comparacion de evidencia de productos.

No agrega:

- PDF.
- UI.
- scraping adicional.
- navegacion web nueva.
- clicks.
- login, carrito, compra o pago.
- aceptacion de cookies.

El input son JSON locales ya generados bajo `artifacts/product-evidence/`.

`sourceArtifactCount` refleja todos los artifacts presentes en esa carpeta al momento de la corrida. Puede incluir artifacts acumulados de validaciones previas, porque `artifacts/` es una carpeta local ignorada por Git.

## Accion nueva

Nombre final:

- `artifact.summarizeProductEvidence`

La accion:

- lee artifacts JSON locales de `artifacts/product-evidence/`;
- ignora/reporta artifacts invalidos como diagnostico controlado;
- escribe un summary JSON bajo `artifacts/product-evidence-summary/`;
- no abre browser;
- no ejecuta acciones UI;
- no usa red.

## Schema de summary

Version:

- `product-evidence-summary/v1`

Campos principales:

- `schemaVersion`
- `createdAtUtc`
- `sourceArtifactCount`
- `validArtifactCount`
- `invalidArtifactCount`
- `items`
- `totals`
- `invalidArtifacts`
- `notes`

Cada item incluye:

- `recipeId`
- `profileId`
- `sourceUrl`
- `productName`
- `category`
- `price`
- `currency`
- `stock`
- `extractionStatus`
- `extractionConfidence`
- `blockedOrMissingFields`
- `hasPrice`
- `hasCurrency`
- `hasStock`
- `rawSignalCount`
- `safetySummary`
- `artifactPath`

Totales:

- `productsWithPrice`
- `productsMissingPrice`
- `productsWithMediumConfidence`
- `productsWithHighConfidence`
- `productsWithDiagnosticStatus`
- `safetyClicksTotal`
- `safetyPaymentsSignalsTotal`
- `artifactsWithWarnings`

## Regla de precio

El summary no inventa precio.

Si un artifact tiene `evidence.price=null`, el summary conserva:

- `price=null`
- `hasPrice=false`

Aunque el artifact tenga rawSignals como `price=38.18`, eso no se convierte en precio visible normalizado.

## Path safety

El writer:

- lee solo bajo `artifacts/product-evidence/`;
- escribe solo bajo `artifacts/product-evidence-summary/`;
- crea la carpeta de salida si no existe;
- verifica que input/output no escapen de sus raices esperadas;
- si el nombre de salida ya existe, usa un sufijo incremental para no sobrescribir.

## Recipe

Recipe agregada:

- `tools/recipes/product-evidence-summary-report.json`

Pasos:

1. `artifact.summarizeProductEvidence`
2. `note`

No contiene:

- `browser.open`
- `click`
- `safe.click`
- `invoke`
- `type`
- `submit`

## Artifacts

Los summary JSON generados quedan bajo:

- `artifacts/product-evidence-summary/`

`artifacts/` esta ignorado por Git. Estos outputs locales no deben commitearse.

## Tests agregados

- carga 2 artifacts validos.
- cuenta `validArtifactCount=2`.
- conserva `price=null`.
- marca `hasPrice=false`.
- no convierte rawSignals `price=38.18` en precio visible.
- calcula `productsMissingPrice`.
- incluye `blockedOrMissingFields`.
- agrega safety clicks total.
- writer crea carpeta si no existe.
- writer no escribe fuera de `artifacts/product-evidence-summary/`.
- invalid artifact se reporta sin romper si hay artifacts validos.
- empty folder produce summary diagnostico vacio.

## Safety

Confirmado por diseno:

- 0 browser nuevo en summary.
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

## Validacion

Validacion ejecutada en branch `hito-062-063-evidence-report-summary`:

- `dotnet build OneBrain.slnx`: OK, 0 errors, 0 warnings.
- `dotnet test`: OK, 120/120 PASS.
- `exit-code-negative`: `Success=false`, `NEGATIVE_EXIT_CODE=1`.
- `suministrosroca-product-readonly-report`: OK, genero artifact fresco.
- `sodimac-product-readonly-report`: OK, genero artifact fresco.
- `product-evidence-summary-report` dry-run: OK, 0 sensitive, sin side effects.
- `product-evidence-summary-report` run: OK, genero summary JSON local.
- `mercadolibre-product-readonly`: OK.
- `product-search-report`: OK.

No se ejecuta Firefox fixture porque sigue quarantined diagnostic/flaky/autoRegression=false.

## Resultado de summary

Artifact generado:

- `artifacts/product-evidence-summary/20260611-201204-product-evidence-summary.json`

Conteos observados:

- `sourceArtifactCount`: 12
- `validArtifactCount`: 12
- `invalidArtifactCount`: 0
- `productsMissingPrice`: 12
- `productsWithPrice`: 0
- `productsWithMediumConfidence`: 12
- `productsWithHighConfidence`: 0
- `productsWithDiagnosticStatus`: 0
- `safetyClicksTotal`: 0
- `safetyPaymentsSignalsTotal`: 0
- `artifactsWithWarnings`: 10

Nota del summary:

- `rawSignals are not treated as visible normalized price`
