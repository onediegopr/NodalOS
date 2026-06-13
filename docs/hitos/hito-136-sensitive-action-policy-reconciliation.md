# HITO-136 — Sensitive Action Policy Reconciliation

## Objetivo del hito

Unificar la clasificacion de sensibilidad del eje `recipe step kind` en una unica fuente canonica:

- `SensitiveActionClassifier`

Sin mezclar ese eje con el vocabulario semantico de negocio de approvals.

## Inventario de fuentes

### Eje A — semantic approval action kinds

Fuente:

- `ApprovalPolicy.SensitiveActionKinds`

Lista actual:

- `send`
- `submit`
- `delete`
- `publish`
- `pay`
- `purchase`
- `login`
- `accept_terms`
- `accept_cookies`
- `modify_financial_data`
- `modify_legal_data`
- `run_script`
- `install_software`

Esto describe acciones de aprobacion de negocio. No describe `recipe step kinds`.

### Eje B — recipe step kinds

Fuentes previas:

- `RecipeRunner.IsSensitiveStep`
- `Program.IsSensitiveKind`
- `SensitiveActionClassifier`

Divergencia real encontrada antes de este hito:

- `RecipeRunner` consideraba sensible `app.close`
- `Program` no lo consideraba sensible

## Decision aplicada

- `app.close` queda canonicamente `Sensitive`
- es el cambio mas conservador
- no se depreca
- `unknown kind` queda `Unknown` y se trata como sensible en `IsSensitiveStepKind`

## Lista canonica de sensitive step kinds

- `actv.invoke`
- `actv.type`
- `key`
- `app.open`
- `app.close`
- `browser.open`
- `browser.close`
- `safe.click`

## Lista canonica de benign step kinds

Enumerada desde el dispatch real de `RecipeRunner`:

- `approval.manifest`
- `artifact.summarizeproductevidence`
- `artifact.writeproductevidence`
- `assert.contains`
- `assert.equals`
- `browser.read`
- `debug.hang`
- `delay`
- `diagnose.msaa`
- `discover.actionableelements`
- `extract.productevidence`
- `extract.visiblefields`
- `if`
- `note`
- `plan.safenavigation`
- `preflight.click`
- `profile.load`
- `report.writeproductevidencehtml`
- `report.writeproductevidencemarkdown`
- `sleep`
- `snapshot.read`
- `visual.capture`
- `visual.capture.element`
- `visual.capture.window`
- `visual.verify.changed`
- `wait`

Nota:

- `diagnose.uia` no figura hoy como `recipe step kind` en el dispatch real del runner, por eso no se agrego a la lista canonica.

## Que se absorbio

- lista hardcodeada de `RecipeRunner.IsSensitiveStep`
- lista hardcodeada de `Program.IsSensitiveKind`

Ambas quedaron delegando al clasificador canonico.

## Politica aplicada

- `trim + lowercase invariant`
- `null`, `""`, whitespace => `Unknown`
- `Unknown` => sensible en `IsSensitiveStepKind`
- fail-closed por defecto

## Que NO se hizo

- no migracion de `safe.click`
- no `WebTargetResolver`
- no `RegionSelector`
- no `BasicActionVerifier`
- no cambios en `SafeExecutionFsm`
- no cambios en `UiaPatternExecutor`
- no `SendInput`
- no action kinds nuevos

## Por que safe.click queda pendiente

- `UiaPatternExecutor` solo soporta `UIA Button Invoke`
- `safe.click` depende de `WebTargetResolver`
- la aprobacion actual de `safe.click` sigue siendo string-based, no `ElementIdentity`-based

## Prerrequisitos de HITO-137+

- P1: superficie de executor segura para la slice web
- P2: identity-at-approval para `safe.click`
- P3: migracion de `WebTargetResolver` hacia el engine generalizable

## Tests

### OneBrain.Safety.Tests

- clasificacion de los 8 sensibles
- `app.close` sensible
- `unknown` fail-closed
- null/empty/whitespace fail-closed
- normalizacion de casing
- approval kinds semanticos no se vuelven benignos
- convergencia del eje step-kind

### OneBrain.Recipes.Tests

- `RecipeRunner` y `Program` delegan al mismo clasificador
- `app.close` sensible en ambos
- benign step kinds se mantienen benignos
- unknown kind sigue sensible

## Validaciones

- `dotnet build OneBrain.slnx`
- `dotnet test OneBrain.slnx`
- smoke:
  - `diagnose baseline --process process-that-does-not-exist-onebrain --iterations 2`
- secret scan sobre diff del hito
- `artifacts/` ignorado/no trackeado
