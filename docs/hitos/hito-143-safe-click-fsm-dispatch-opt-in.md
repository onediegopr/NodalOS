# HITO-143 - safe.click FSM Dispatch Opt-In

## Objetivo

Migrar `safe.click` a un path nuevo gobernado por `SafeExecutionFsm`, pero solo cuando el step pida explicitamente:

* `dispatchPath=safe-executor`

El path legacy de `safe.click` queda intacto cuando `dispatchPath` no esta presente.

## Inventario previo

### Flujo actual de `safe.click` legacy

En `RecipeRunner.cs` el flujo legacy sigue siendo:

1. leer `targetText`, `approvalPrefix`, `mode`
2. `ClickPreflightEvaluator`
3. `ValidateApprovalBinding`
4. `executionAllowedInThisHito`
5. `WebTargetResolver.Resolve`
6. reattach por `WebTargetResolver.FindElementByName`
7. `InvokePattern`
8. fallback `el.Click`
9. fallback desktop via `UiaActionExecutor`

Ese flujo no usa `SafeExecutionFsm` y no enforcea binding v3.

### Flujo nuevo propuesto

El path nuevo se habilita solo con:

* `dispatchPath=safe-executor`

Precondiciones operativas:

* `preflight.click`
* `target.observe`
* `approval.manifest` v3 fuerte
* `safe.click dispatchPath=safe-executor`

Luego compone:

* `ApprovalBindingValidator`
* `RecipeSafetyContract`
* `ContractValidator`
* `SafeExecutionFsm`
* `UiaPatternExecutor`
* `SafeClickStepVerifier`

Sin fallback inseguro.

## Que no se toca

* `approval-v2`
* `evidenceHash`
* `policyVersion`
* `ValidateApprovalBinding`
* `RegionSelector`
* `BasicActionVerifier`
* `el.Click` legacy
* `UiaActionExecutor` legacy

## dispatchPath opt-in

`safe.click` ahora tiene dos paths:

* sin `dispatchPath`: legacy exacto
* `dispatchPath=safe-executor`: path nuevo FSM

Un valor distinto de vacio y distinto de `safe-executor` se bloquea con `PolicyDenied` para evitar typos silenciosos.

## Enforcement del path nuevo

El path nuevo exige:

* `approval.identity.schemaVersion = approval-v3`
* `approval.identity.strength = Strong`
* `approval.identity.digest`
* `approval.identity.selector`
* `approval.identity.bindingHash`
* `identity.source = web-uia`

Casos bloqueados:

* `approval-v2` o manifest ausente
* identidad weak
* `RuntimeId` cambiado
* target ambiguo
* `InvokePattern` no soportado
* role fuera de allowlist

`safe.click` sigue siendo irreversible. No se acepta `LikelySame`.

## Uso de SafeExecutionFsm

El path nuevo:

1. conserva los gates legacy previos
2. resuelve target actual con `WebTargetResolver.Resolve`
3. compone `RecipeSafetyContract`
4. construye `ApprovalBinding` con `TryBuildApprovalBinding`
5. ejecuta `SafeExecutionFsm`
6. despacha solo por `UiaPatternExecutor`
7. verifica con `SafeClickStepVerifier`

No usa:

* `el.Click`
* `UiaActionExecutor`
* `SendInput`
* coordenadas
* `GetClickablePoint`

## RootHwnd para contenido web

`PatternExecutionRequest` se extiende aditivamente con `RootHwnd`.

`UiaPatternExecutor` ahora prefiere `RootHwnd` cuando esta presente. Esto evita depender del top-level window cuando el contenido web vive en un child HWND del browser.

Si `RootHwnd` no esta presente, el executor conserva su comportamiento existente.

## Variables nuevas

Variables aditivas del path nuevo:

* `safeClick.fsm.finalState`
* `safeClick.fsm.failureKind`
* `safeClick.fsm.blockReason`
* `safeClick.fsm.success`
* `safeClick.fsm.transitionCount`
* `safeClick.fsm.reasons`
* `safeClick.fsm.ledgerJson`

No cambian:

* `safeClick.*`
* `safeClick.plan.*`
* `resolution.*`
* `approval.*`

## Que no se hizo

* no strict default
* no retiro global de `el.Click`
* no retiro global de `UiaActionExecutor`
* no cambios a `approval-v2`
* no cambios a `ValidateApprovalBinding`
* no `RegionSelector`
* no `BasicActionVerifier`

## Proximo hito

`HITO-144 - flip default + el.Click retirement`
