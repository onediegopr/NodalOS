# HITO-147 — Gradual Enablement (web, eligible-only)

## Objetivo

Empezar el rollout real de `safe.click` hacia `SafeExecutionFsm`, pero **solo** para steps web
estrictamente elegibles. No es un flip general. La ejecución real solo cambia para un `safe.click`
web que, sin `dispatchPath`, cumpla el predicado endurecido de elegibilidad y con el kill-switch en
`web-eligible`. Todo lo demás sigue legacy, sin cambios.

- No flip general.
- No afecta desktop.
- No retira legacy, `el.Click` ni `UiaActionExecutor`.
- No fallback silencioso ante un block del FSM.

## Por qué no es un flip general

La auditoría previa (H143→H146) concluyó que el path FSM es correcto y fail-closed, pero faltaba mover
tráfico con evidencia y de forma determinista. El rollout se hace **por elegibilidad per-step** (no por
porcentaje de tráfico), acotado a **web** (la identidad web está madura; desktop solo tiene identidad
shadow, sin path de dispatch real). Un step que no es elegible no se rompe: queda en legacy.

## Predicado FSM Eligible endurecido

`SafeClickShadowReadinessEvaluator` ahora exige, además de las condiciones previas:

Condiciones previas (se mantienen):

- `HasApprovalV3 == true`
- `HasTargetObserve == true`
- `IdentityStrength == Strong`
- `HasRuntimeId == true`
- `RuntimeIdentityMatch == Same`
- `ProjectedState == Bound`
- `ContractValid == true`
- `WouldUseUnsafeFallback == false`

Condiciones nuevas (HITO-147):

- `InvokePatternAvailable == true` (no `Unknown`, no `false`)
- `RoleAllowedForSafeExecutor == true` (rol del target observado ∈ allowlist `ExecutorSurfacePolicy`)
- `IdentitySource == "web-uia"` (desktop nunca es default-eligible en este hito)

Si falta cualquier dato: no eligible ⇒ legacy (salvo `dispatchPath=safe-executor` explícito).
Razones nuevas: `InvokePatternUnavailable`, `RoleNotAllowed`, `NotWebUia`.

## Kill-switch global (local-first)

Variable de entorno `ONEBRAIN_SAFE_CLICK_FSM_DEFAULT`:

- `disabled` (default seguro): `safe.click` sin `dispatchPath` usa legacy.
- `web-eligible`: `safe.click` sin `dispatchPath` puede usar FSM solo si es web-eligible.
- `legacy`: fuerza legacy explícito (revert inmediato sin redeploy).

Si la variable no está seteada o tiene un valor desconocido ⇒ `disabled` (fail-safe).

## Enrutamiento de `ExecuteSafeClick`

- `dispatchPath=safe-executor` → path FSM opt-in (sin cambios, HITO-143).
- `dispatchPath=legacy` → legacy + variables deprecadas (opt-out explícito).
- `dispatchPath` desconocido → `PolicyDenied`.
- Sin `dispatchPath` y kill-switch `disabled`/`legacy` → legacy (sin cambio funcional).
- Sin `dispatchPath` y kill-switch `web-eligible`:
  - si web-eligible por manifest (`ValidateSafeExecutorManifest` OK + `web-uia`) → FSM, **sin fallback** si bloquea.
  - si no → legacy, marcando readiness/reason.

## No fallback silencioso

Si un step fue enrutado a FSM por default y el FSM bloquea/falla:

- NO se reintenta legacy, `el.Click`, `UiaActionExecutor`, `SendInput`, coordenadas ni `GetClickablePoint`.
- Queda evidencia: `safeClick.fsm.routedByDefault=true`, `safeClick.fsm.defaultRouteReason=WebEligible`,
  `safeClick.fsm.blockedWithoutLegacyFallback=true`.

## Variables aditivas nuevas

- `safeClick.fsm.defaultEnabled`
- `safeClick.fsm.defaultMode`
- `safeClick.fsm.routedByDefault`
- `safeClick.fsm.defaultRouteReason`
- `safeClick.fsm.defaultRouteEligible`
- `safeClick.fsm.defaultRouteScope`
- `safeClick.fsm.blockedWithoutLegacyFallback`
- `safeClick.legacy.explicitOptOut`
- `safeClick.legacy.deprecated`
- `safeClick.legacy.reason`

## Métricas aditivas nuevas

- `safeClick.migration.defaultFsmEnabled`
- `safeClick.migration.defaultFsmRouted`
- `safeClick.migration.defaultFsmEligibleButNotEnabled`
- `safeClick.migration.defaultFsmBlocked`
- `safeClick.migration.explicitLegacyOptOut`
- `safeClick.migration.desktopExcludedFromDefault`
- `safeClick.migration.unknownDispatchPathBlocked`

## Qué no cambia

- approval-v2, `evidenceHash`, `policyVersion`, `ValidateApprovalBinding`.
- `RegionSelector.FindBestMatch`, `BasicActionVerifier.TargetExists`.
- `el.Click`, `UiaActionExecutor` (siguen presentes en el path legacy).
- Sin `SendInput`, coordenadas, `GetClickablePoint`, OCR ni fallback visual.
- Sin ejecución desktop FSM (desktop excluido del default).

## Qué queda para H148

- Web Stabilization + RuntimeId Hardening: medir `RuntimeIdStableRate` web sobre ventana, reducir el
  churn observe→dispatch y ampliar la cohorte web según gates de cohorte.
- Luego: path de dispatch desktop real (H149), enablement desktop (H150), deprecation y retiro de legacy.
