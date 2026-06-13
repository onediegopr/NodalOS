# HITO-135 — Safe Executor Contract

## Objetivo

Convertir el flujo procedural del executor harness benigno en un flujo gobernado por una maquina de estados fail-closed, donde un paso avanza solo si la transicion esta permitida y queda evidencia escrita.

## Inventario previo

### Ubicaciones relevadas

- `ExecutorHarnessService.ExecuteSupervisedClick`
- `ExecutorHarnessSafetyMatrix`
- `RecipeRunner.ValidateApprovalBinding`
- `ApprovalManifestBuilder`
- `RecipeRunner.IsSensitiveStep`
- `Program.IsSensitiveKind`
- `ApprovalPolicy.SensitiveActionKinds`
- `UiaActionExecutor`
- `ForegroundWindowReader`

### Duplicados de sensibilidad detectados

- `RecipeRunner.IsSensitiveStep` incluye:
  - `actv.invoke`
  - `actv.type`
  - `key`
  - `app.open`
  - `app.close`
  - `browser.open`
  - `browser.close`
  - `safe.click`
- `Program.IsSensitiveKind` incluye:
  - `actv.invoke`
  - `actv.type`
  - `key`
  - `app.open`
  - `browser.open`
  - `browser.close`
  - `safe.click`
- `ApprovalPolicy.SensitiveActionKinds` incluye solo acciones de aprobacion de alto riesgo y no contiene `app.close`.

### Punto de parada encontrado

Las listas difieren hoy. Caso visible: `app.close`.

Para no cambiar comportamiento existente:

- se implementa `SensitiveActionClassifier` como inventario canonico y reporte de divergencia
- no se reescriben `RecipeRunner.IsSensitiveStep` ni `Program.IsSensitiveKind` en este hito
- la unificacion de wiring queda pendiente para un hito posterior con decision explicita

### Fuera de alcance confirmado

- no `WebTargetResolver`
- no `RegionSelector.FindBestMatch`
- no `BasicActionVerifier.TargetExists`
- no `safe.click` de recetas
- no recetas productivas

## Que se implemento

### FSM minima

- `StepState`
- `StepTransition`
- `StepTransitionEvidence`
- `EvidenceLedger`
- `CancellationPolicy`
- `SafeExecutionFsm`

Estados:

- `Created`
- `Validated`
- `Bound`
- `Executing`
- `Verifying`
- `Succeeded`
- `Blocked`
- `Paused`
- `Failed`
- `Aborted`

Propiedades clave:

- append-only ledger
- terminales sin salida
- `Paused` no vuelve a `Executing`
- toda terminal de fallo lleva `FailureKind`

### Contrato deny-by-default

- `RecipeSafetyContract`
- `ExecutionWindowConstraints`
- `ContractValidation`
- `ContractValidator`

Reglas:

- campo ausente => invalid / denied
- contrato malformado => `PolicyDenied`
- `MaxActions` debe ser `1`
- `ApprovalRef` es obligatorio
- el `ActionCeiling` no puede exceder el techo de la fuente

### Approval binding

- `ApprovalBinding`
- `ApprovalBindingValidator`
- `ApprovalBindingResult`

Reglas:

- binding atado a `ElementIdentity`
- sin autoridad por `targetText`
- `Same + digest igual` => bound
- `LikelySame` solo permitido si el contrato es reversible explicito
- digests distintos en irreversible => `ApprovalInvalidated`
- ambiguedad => `Ambiguous`
- no `first-wins`

### Desktop ownership

- `IDesktopOwnershipMonitor`
- `OwnershipSnapshot`
- `DesktopOwnershipMonitor`

Implementacion:

- `GetLastInputInfo`
- `ForegroundWindowReader`
- sin hooks
- sin keylogging
- sin bloquear input

Regla operativa:

como este hito solo usa UIA `Invoke`, si avanza `LastInputTick` durante `Executing` o `Verifying`, se asume input humano y la FSM falla cerrado.

### UIA invoke minimo

- `IUiaPatternExecutor`
- `PatternExecutionRequest`
- `PatternExecutionResult`
- `UiaPatternExecutor`

Alcance:

- solo `Invoke`
- sin `Toggle`
- sin `SetValue`
- sin `SendInput`
- sin coordenadas

### Harness absorbido

- `ExecutorHarnessService.ExecuteSupervisedClick` ahora delega la orquestacion a `SafeExecutionFsm`
- se preservan los shapes publicos del harness
- `ExecutorHarnessSafetyMatrix` se mantiene como adapter para dry-run y vistas, sin ampliar surface
- `PilotUiaHarnessClickExecutor` usa `UiaPatternExecutor` para el camino benigno local

### Evidence por transicion

`ExecutorHarnessRunResult` y `ExecutorHarnessEvidenceRecord` ahora pueden incluir:

- `FinalState`
- `FailureKind`
- `TransitionEvidence`
- `SafetyContract`
- `ApprovalBinding`

Esto mantiene compatibilidad hacia atras porque los nuevos campos son opcionales/agregados.

## Que NO se hizo

- no `WebTargetResolver`
- no `RegionSelector`
- no `BasicActionVerifier`
- no `safe.click` path
- no OCR
- no vision
- no CDP
- no `SendInput`
- no action kinds nuevos
- no cambios de runner behavior en CLI
- no cambios de executor fuera del harness benigno

## Legacy absorbido

- flujo procedural de `ExecutorHarnessService.ExecuteSupervisedClick`
- verification y dispatch del harness benigno bajo FSM

## Legacy pendiente

- wiring canonico de `SensitiveActionClassifier` en `RecipeRunner` y `Program`
- absorcion total de `ExecutorHarnessSafetyMatrix`
- migracion de `safe.click`
- verificadores legacy fuera del harness

## Tests agregados

### `OneBrain.Safety.Tests`

- happy path de FSM
- deny-by-default del contrato
- approval binding con mismatch, ambiguedad y reversible
- human input before dispatch
- human input during executing
- divergencia actual del clasificador sensible

### `OneBrain.Recipes.Tests`

- `ExecutorHarnessRunResult` expone transiciones de FSM
- `ExecutorHarnessEvidenceRecord` persiste `FailureKind` y `TransitionEvidence`

## Validaciones

- `dotnet build OneBrain.slnx`
- `dotnet test OneBrain.slnx`
- smoke:
  - `diagnose baseline --process process-that-does-not-exist-onebrain --iterations 2`
- secret scan sobre diff del hito
- `artifacts/` ignorado/no trackeado

## Riesgos pendientes

- `SensitiveActionClassifier` detecta divergencia real y no se conecto todavia para no cambiar behavior
- `ExecutorHarnessSafetyMatrix` sigue coexistiendo como adapter por compatibilidad de vistas y mensajes
- la verificacion sigue siendo la observable actual del harness; no se introduce un verificador semantico nuevo

## Proximo paso recomendado

HITO-136 o la siguiente rebanada del executor, pero solo despues de publicar esta base y resolver de manera explicita la unificacion de clasificacion sensible.
