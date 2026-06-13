# HITO-144 — safe.click FSM Shadow-Default

## Objetivo

Agregar observabilidad shadow para medir compatibilidad real del corpus actual con el path `safe-executor` sin cambiar la ejecución real de `safe.click`.

## Qué exige hoy `safe-executor`

- `dispatchPath = safe-executor`
- preflight legacy válido
- `ValidateApprovalBinding` legacy válido
- `executionAllowedInThisHito = true`
- manifest `approval-v3`
- identidad `Strong`
- selector aprobado
- `identityBindingHash`
- target resuelto en web con `RuntimeId`
- `InvokePattern` disponible sobre rol allowlisted

## Qué métricas faltaban para migrar

- porcentaje del corpus elegible para FSM
- porcentaje que sigue en `approval-v2`
- porcentaje con identidad débil o sin `RuntimeId`
- estabilidad entre identidad aprobada y observada
- uso real de `el.Click`
- uso real de `UiaActionExecutor`
- porcentaje de casos que requerirían fallback legacy

## Qué mide este hito

- `safeClick.fsmReady.*` para todo `safe.click`, con y sin `dispatchPath`
- `runtimeIdentityMatch` entre identidad aprobada y observada
- uso real de `el.Click`
- uso real de `UiaActionExecutor`
- uso real de fallback legacy inseguro
- elegibilidad de un click para el path FSM sin ejecutar FSM

## Flujo shadow agregado

1. `safe.click` corre exactamente como hoy.
2. En paralelo, `SafeClickPlanner` calcula `ProjectedState`, binding y necesidad de fallback.
3. `SafeClickShadowReadinessEvaluator` transforma ese plan en readiness legible:
   - `ApprovalV2`
   - `WeakIdentity`
   - `RuntimeIdMissing`
   - `RuntimeIdChanged`
   - `WouldUseLegacyFallback`
   - `Ready`
4. La ejecución real no cambia.

## Variables nuevas

- `safeClick.fsmReady.success`
- `safeClick.fsmReady.reason`
- `safeClick.fsmReady.projectedState`
- `safeClick.fsmReady.identityStrength`
- `safeClick.fsmReady.hasTargetObserve`
- `safeClick.fsmReady.hasApprovalV3`
- `safeClick.fsmReady.hasRuntimeId`
- `safeClick.fsmReady.runtimeIdentityMatch`
- `safeClick.fsmReady.wouldRequireLegacy`
- `safeClick.fsmReady.eligible`
- `safeClick.fsmReady.summary`

## Instrumentación legacy

- `safeClick.legacy.usedElClick`
- `safeClick.legacy.usedUiaActionExecutor`
- `safeClick.legacy.usedUnsafeFallback`
- `safeClick.legacy.summary`

## Qué NO cambia

- no flip default
- no retiro de legacy
- no retiro de `el.Click`
- no retiro de `UiaActionExecutor`
- no dispatch FSM nuevo cuando no fue pedido
- no cambios en `approval-v2`
- no cambios en `evidenceHash`
- no cambios en `policyVersion`
- no cambios en `ValidateApprovalBinding`
- no cambios en `RegionSelector`
- no cambios en `BasicActionVerifier`
- no `SendInput`
- no coordenadas
- no `GetClickablePoint`

## Qué habilita para HITO-145 / HITO-146

- medir qué parte del corpus ya tiene `approval-v3` fuerte
- medir cuánto legacy depende todavía de `el.Click`
- medir cuánto legacy depende todavía de `UiaActionExecutor`
- medir cuántos casos tienen `RuntimeId` estable
- decidir con evidencia cuándo hacer flip default y cuándo retirar legacy
