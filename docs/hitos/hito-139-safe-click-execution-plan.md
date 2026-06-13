# HITO-139 — Safe Click Execution Plan

## Objetivo

Agregar un planner puro y fail-closed que proyecte, en shadow y sin ejecutar nada, qué haría el flujo seguro futuro de `safe.click`.

El planner:

- compone `RecipeSafetyContract`
- intenta construir `ApprovalBinding` desde manifest v3
- usa `ContractValidator`
- usa `ApprovalBindingValidator`
- usa `WebCandidateMapper`
- devuelve una proyección diagnóstica

No hace dispatch. No hace clicks. No cambia el resultado real de `safe.click`.

## Flujo actual de `safe.click`

Hoy `safe.click`:

1. valida `targetText`
2. verifica sesión browser owned cuando corresponde
3. ejecuta preflight
4. valida binding legacy `approval-v2`
5. chequea `executionAllowedInThisHito`
6. resuelve por `WebTargetResolver` en browser owned
7. ejecuta click real con `Invoke` o `el.Click()`
8. si no aplica, cae a `UiaActionExecutor`

## Qué proyecta el planner

`SafeClickPlanner.Plan(...)` devuelve:

- `ProjectedState`
- `FailureKind`
- `BlockReason`
- `IdentityStrength`
- `ContractValid`
- `BindingVerdict`
- `ParityAgrees`
- `WouldDispatch`
- `WouldUseUnsafeFallback`
- `Reasons`

En HITO-139:

- `WouldDispatch` siempre es `false`
- la proyección es solo diagnóstica
- el flujo real no se altera

## Por qué no se migra todavía

No se migra `safe.click` a ejecución segura real todavía porque:

- `UiaPatternExecutor` no se amplía en este hito
- el path browser actual sigue usando `el.Click()` y fallback legacy
- la identidad web sigue siendo débil en la mayoría de los casos
- `ApprovalBindingValidator` exige `Same` para irreversible

El bloqueo real para enforcement no es el planner: es la debilidad de identidad web y la superficie legacy de ejecución.

## Por qué no hay dispatch

El hito es shadow-only por diseño:

- no toca `UiaPatternExecutor`
- no toca `SafeExecutionFsm`
- no toca `ValidateApprovalBinding`
- no toca `approval-v2`
- no toca `evidenceHash`
- no toca `policyVersion`

## Qué usa

- `RecipeSafetyContract`
- `ContractValidator`
- `ApprovalBinding`
- `ApprovalBindingValidator`
- `ApprovalManifestBuilder.TryBuildApprovalBinding`
- `WebCandidateMapper`
- `ElementIdentity`
- `ElementMatcher`
- `ElementFingerprintBuilder`

## Variables nuevas

Se agregan variables aditivas:

- `safeClick.plan.projectedState`
- `safeClick.plan.failureKind`
- `safeClick.plan.blockReason`
- `safeClick.plan.identityStrength`
- `safeClick.plan.contractValid`
- `safeClick.plan.bindingVerdict`
- `safeClick.plan.parityAgrees`
- `safeClick.plan.wouldDispatch`
- `safeClick.plan.wouldUseUnsafeFallback`
- `safeClick.plan.reasons`

## Qué no se hizo

- no `safe.click` migration
- no execution changes
- no `UiaPatternExecutor` changes
- no `el.Click()` changes
- no FSM changes
- no enforcement
- no `approval-v2` changes
- no `ValidateApprovalBinding` changes
- no `WebTargetResolver` behavior changes
- no `RegionSelector`
- no `BasicActionVerifier`
- no clicks

## Orden recomendado

1. HITO-140 — Web Identity Strengthening
2. HITO-141 — Safe Executor Surface + `el.Click()` retirement
3. HITO-142 — `safe.click` FSM dispatch migration + enforcement
