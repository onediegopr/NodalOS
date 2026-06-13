# HITO-151 - Desktop Gradual Enablement eligible-only

## Objetivo

HITO-151 habilita `safe.click` desktop por `SafeExecutionFsm` como default solamente cuando el step desktop es estrictamente elegible.

No hay flip general, no se retira legacy, no se retira `el.Click`, no se retira `UiaActionExecutor` y no existe fallback silencioso a legacy si FSM bloquea.

## Kill-switch Extendido

`ONEBRAIN_SAFE_CLICK_FSM_DEFAULT` acepta:

* `disabled`
* `legacy`
* `web-eligible`
* `desktop-eligible`
* `all-eligible`

Default seguro:

```text
disabled
```

Valores vacios o desconocidos:

```text
disabled
```

## Semantica de Routing

### disabled / legacy

`safe.click` sin `dispatchPath` usa legacy siempre.

### web-eligible

Solo web eligible puede ir a FSM default.

Desktop sin `dispatchPath` sigue legacy, aunque sea desktop eligible.

### desktop-eligible

Solo desktop eligible puede ir a FSM default.

Web sin `dispatchPath` sigue legacy.

### all-eligible

Web eligible y desktop eligible pueden ir a FSM default.

Los demas steps siguen legacy.

## Predicado DesktopEligibleForFsm

Desktop default exige:

* `HasApprovalV3 == true`
* `HasTargetObserve == true`
* `IdentityStrength == Strong`
* `HasRuntimeId == true`
* `RuntimeIdentityMatch == Same`
* `ProjectedState == Bound`
* `ContractValid == true`
* `WouldUseUnsafeFallback == false`
* `InvokePatternAvailable == true`
* `RoleAllowedForSafeExecutor == true`
* `IdentitySource == uia`
* `DesktopRootAvailable == true`

No acepta:

* `Weak`
* `None`
* `LikelySame`
* `Missing`
* `Unknown`
* `RuntimeIdChanged`
* rol no permitido
* falta de `InvokePattern`
* falta de `RootHwnd`

## Runtime Stability Desktop

Antes del dispatch default desktop:

```text
approval identity fuerte
    ↓
re-observe desktop target
    ↓
RuntimeId/digest siguen Same
    ↓
FSM dispatch
```

Si cambia RuntimeId, falta identidad o el target no se puede re-observar:

* bloquea fail-closed
* `FailureKind = Stale` cuando corresponde
* no hay fallback legacy
* no hay `el.Click`
* no hay `UiaActionExecutor`

## Variables Aditivas

Variables generales:

* `safeClick.fsm.defaultMode`
* `safeClick.fsm.defaultEnabled`
* `safeClick.fsm.routedByDefault`
* `safeClick.fsm.defaultRouteEligible`
* `safeClick.fsm.defaultRouteScope`
* `safeClick.fsm.defaultRouteReason`
* `safeClick.fsm.blockedWithoutLegacyFallback`

Variables desktop:

* `safeClick.desktopFsm.defaultEnabled`
* `safeClick.desktopFsm.routedByDefault`
* `safeClick.desktopFsm.defaultRouteEligible`
* `safeClick.desktopFsm.defaultRouteReason`
* `safeClick.desktopFsm.defaultRouteScope`
* `safeClick.desktopFsm.blockedWithoutLegacyFallback`
* `safeClick.desktopFsm.runtimeStabilityChecked`
* `safeClick.desktopFsm.runtimeStabilityVerdict`
* `safeClick.desktopFsm.reobserveAttempted`
* `safeClick.desktopFsm.reobserveSucceeded`
* `safeClick.desktopFsm.blockedByStaleIdentity`

## Metricas

Metricas nuevas:

* `safeClick.migration.desktopDefaultFsmEnabled`
* `safeClick.migration.desktopDefaultFsmRouted`
* `safeClick.migration.desktopDefaultEligibleButNotEnabled`
* `safeClick.migration.desktopDefaultBlocked`
* `safeClick.migration.desktopDefaultBlockedByStaleIdentity`
* `safeClick.migration.allEligibleModeEnabled`
* `safeClick.migration.defaultFsmScopeWeb`
* `safeClick.migration.defaultFsmScopeDesktop`

Son locales, aditivas y sin telemetria externa.

## Que No Cambia

* no flip general inseguro
* desktop solo eligible-only
* web eligible-only se mantiene
* no retiro legacy
* no retiro de `el.Click`
* no retiro de `UiaActionExecutor`
* no fallback silencioso
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

* `disabled` y `legacy` mantienen desktop legacy
* `web-eligible` no enruta desktop
* `desktop-eligible` enruta desktop eligible
* `desktop-eligible` mantiene desktop ineligible en legacy
* `all-eligible` enruta web eligible y desktop eligible
* desktop default exige approval-v3, identidad fuerte, RuntimeId, Same, InvokePattern, rol permitido y RootHwnd
* desktop default re-observa antes de dispatch
* runtime cambiado/faltante bloquea antes de dispatch
* bloqueo default no cae a legacy
* no `el.Click`
* no `UiaActionExecutor`
* opt-in y `dispatchPath=legacy` siguen funcionando
* invariantes de approval-v2/evidenceHash/policyVersion/ValidateApprovalBinding

## Proximo Hito

H152 - Legacy Deprecation.
