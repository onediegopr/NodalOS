# HITO-148 - Web Stabilization + RuntimeId Hardening

## Objetivo

Endurecer el rollout web eligible-only antes de ampliar scope o deprecar legacy.

HITO-148 no hace flip general, no habilita desktop FSM, no retira legacy, no retira `el.Click` y no retira `UiaActionExecutor`.

El foco es reducir riesgo entre `target.observe`/approval y dispatch:

```text
approval identity fuerte
    ↓
re-observe web target justo antes del dispatch default
    ↓
RuntimeId/digest siguen Same
    ↓
FSM dispatch
```

Si la identidad cambió, falta o queda ambigua, el default FSM bloquea fail-closed y no cae a legacy.

## RuntimeId Stability Model

Se agrega `SafeClickRuntimeStability` como modelo core puro para describir estabilidad de identidad:

* `ApprovedDigest`
* `ObservedDigest`
* `Match`
* `RuntimeIdPresent`
* `RuntimeIdChanged`
* `IdentitySource`
* `ObserveAgeMs`
* `ReobserveAttempted`
* `ReobserveSucceeded`
* `ReobserveMatch`
* `StabilityVerdict`
* `BlockReason`

Valores de verdict:

* `Stable`
* `Changed`
* `Missing`
* `Unknown`
* `ReobservedStable`
* `ReobservedChanged`

La evaluación acepta únicamente `Same` fuerte. `LikelySame` no habilita dispatch para `safe.click` irreversible.

## Re-observe Before Default Dispatch

Sólo aplica cuando:

* no hay `dispatchPath`
* kill-switch `ONEBRAIN_SAFE_CLICK_FSM_DEFAULT=web-eligible`
* el manifest es web eligible
* el source aprobado es `web-uia`

No aplica a:

* legacy normal
* `dispatchPath=legacy`
* `dispatchPath=safe-executor` opt-in
* kill-switch `disabled`
* kill-switch `legacy`
* desktop

El re-observe usa `WebTargetResolver.Resolve` mediante el mismo punto de resolución de `safe.click`, recalcula identidad y compara con la identidad aprobada. Si es estable, esa misma resolución reobservada se reutiliza para el dispatch FSM.

## Stale / ApprovalInvalidated

Si cambia RuntimeId o digest:

* `failureKind = Stale`
* `blockReason = ApprovalInvalidated`
* `safeClick.runtimeStability.verdict = ReobservedChanged`
* `safeClick.fsm.blockedWithoutLegacyFallback = true`

Si falta identidad fuerte:

* `failureKind = Stale`
* `blockReason = ApprovalInvalidatedMissingIdentity`
* `safeClick.runtimeStability.verdict = Missing`

Si el target desaparece o queda ambiguo:

* `NotFound` o `Ambiguous` según resolución
* no dispatch
* no fallback legacy

## Variables Aditivas

Variables por step:

* `safeClick.runtimeStability.verdict`
* `safeClick.runtimeStability.observeAgeMs`
* `safeClick.runtimeStability.reobserveAttempted`
* `safeClick.runtimeStability.reobserveSucceeded`
* `safeClick.runtimeStability.reobserveMatch`
* `safeClick.runtimeStability.blockReason`

Métricas:

* `safeClick.migration.runtimeStabilityChecked`
* `safeClick.migration.runtimeStable`
* `safeClick.migration.runtimeChanged`
* `safeClick.migration.runtimeMissing`
* `safeClick.migration.reobserveAttempted`
* `safeClick.migration.reobserveSucceeded`
* `safeClick.migration.reobserveChanged`
* `safeClick.migration.defaultBlockedByStaleIdentity`
* `safeClick.migration.defaultBlockedByMissingIdentity`

## Qué No Cambia

* no scope expansion
* no desktop FSM
* no retiro de legacy
* no retiro de `el.Click`
* no retiro de `UiaActionExecutor`
* no fallback silencioso a legacy si FSM bloquea
* no `SendInput`
* no coordenadas
* no `GetClickablePoint`
* no OCR
* no cambios a `approval-v2`
* no cambios a `evidenceHash`
* no cambios a `policyVersion`
* no cambios a `ValidateApprovalBinding`
* no cambios a `RegionSelector.FindBestMatch`
* no cambios a `BasicActionVerifier.TargetExists`

## Tests

Cobertura agregada:

* re-observe antes del dispatch default web eligible
* runtime estable continúa a FSM
* runtime cambiado bloquea antes del dispatch
* runtime faltante bloquea antes del dispatch
* `LikelySame` no se acepta
* fallo de re-observe bloquea fail-closed
* bloqueo de re-observe no cae a legacy
* no llama `el.Click`
* no llama `UiaActionExecutor`
* variables y métricas de estabilidad
* legacy, kill-switch disabled y desktop no reobservan

## Próximo Hito

H149 - Desktop FSM Dispatch Path shadow/opt-in.
