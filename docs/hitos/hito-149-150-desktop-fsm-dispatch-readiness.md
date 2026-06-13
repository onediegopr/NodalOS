# HITO-149/150 - Desktop FSM Dispatch Path + Desktop Gradual Readiness

## Objetivo

HITO-149 agrega un path real desktop opt-in para `safe.click` usando `SafeExecutionFsm`, identidad de `target.observe.desktop`, `approval-v3` fuerte, `ApprovalBindingValidator` y `UiaPatternExecutor`.

HITO-150 agrega readiness y metricas desktop para preparar gradual enablement posterior, sin habilitar default desktop.

No hay flip desktop, no se retira legacy, no se retira `el.Click`, no se retira `UiaActionExecutor` y no existe fallback silencioso a legacy cuando FSM bloquea.

## Flujo Desktop FSM Opt-In

El path nuevo solo se activa explicitamente con:

```text
safe.click dispatchPath=safe-executor identitySource=uia
```

o cuando el manifest aprobado trae:

```text
approval.identity.source = uia
```

Flujo esperado:

```text
target.observe.desktop
    ↓
approval.manifest v3 strong source=uia
    ↓
safe.click dispatchPath=safe-executor
    ↓
RecipeSafetyContract
    ↓
ApprovalBindingValidator
    ↓
SafeExecutionFsm
    ↓
UiaPatternExecutor
    ↓
SafeClickStepVerifier
```

## Gates Requeridos

El path desktop FSM opt-in bloquea fail-closed si falta cualquiera de estos requisitos:

* `approval-v3` fuerte
* `IdentitySource == uia`
* `RuntimeId` presente
* `RuntimeIdentityMatch == Same`
* contrato valido
* `InvokePattern` disponible
* rol permitido por `ExecutorSurfacePolicy`
* root desktop confiable (`RootHwnd`)
* target observado por `target.observe.desktop`
* sin unsafe fallback

No se acepta `Weak`, `LikelySame`, approval-v2 ni identidad faltante para `safe.click` irreversible en este path.

## Desktop Root

`target.observe.desktop` ahora propaga `RootHwnd` de forma aditiva.

El path desktop FSM exige root confiable antes de construir el dispatch request. Si no hay root:

```text
FailureKind = PolicyDenied
blockReason = DesktopRootRequired
```

No se cae a busqueda global insegura, coordenadas ni fallback visual.

## Variables Aditivas

Variables desktop FSM:

* `safeClick.desktopFsm.enabled`
* `safeClick.desktopFsm.eligible`
* `safeClick.desktopFsm.identitySource`
* `safeClick.desktopFsm.rootHwndPresent`
* `safeClick.desktopFsm.routedOptIn`
* `safeClick.desktopFsm.blockReason`
* `safeClick.desktopFsm.verdict`

Variables de readiness:

* `safeClick.fsmReady.desktopEligible`
* `safeClick.fsmReady.desktopRootAvailable`

Variables de migracion desktop:

* `safeClick.migration.desktopEligibleForFsm`
* `safeClick.migration.desktopNotEligibleForFsm`
* `safeClick.migration.desktopRuntimeStable`
* `safeClick.migration.desktopRuntimeChanged`
* `safeClick.migration.desktopInvokePatternAvailable`
* `safeClick.migration.desktopRoleAllowed`
* `safeClick.migration.desktopRootAvailable`
* `safeClick.migration.desktopOptInRouted`
* `safeClick.migration.desktopOptInBlocked`

Variables de identidad desktop propagadas al approval:

* `approval.identity.rootHwnd`
* `approval.identity.hasInvoke`

Estas variables son metadata aditiva; no alimentan `evidenceHash` ni cambian `approval-v2`.

## Readiness Desktop

`DesktopEligibleForFsm` es independiente de `EligibleForFsm` web.

Debe cumplirse:

* `HasApprovalV3`
* `HasTargetObserve`
* `IdentityStrength == Strong`
* `HasRuntimeId`
* `RuntimeIdentityMatch == Same`
* `ProjectedState == Bound`
* `ContractValid`
* `WouldUseUnsafeFallback == false`
* `InvokePatternAvailable == true`
* `RoleAllowedForSafeExecutor == true`
* `IdentitySource == uia`
* `DesktopRootAvailable == true`

Aunque sea desktop eligible, sin `dispatchPath=safe-executor` sigue usando legacy. No existe default desktop.

## Default Desktop

HITO-149/150 no agrega modo default desktop.

`ONEBRAIN_SAFE_CLICK_FSM_DEFAULT=web-eligible` sigue aplicando solo a web eligible.

Desktop sin `dispatchPath`:

* ejecuta legacy igual que antes
* calcula readiness y metricas aditivas
* no usa FSM
* no bloquea por readiness

## Que No Cambia

* no desktop default
* no scope expansion insegura
* no retiro de legacy
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

* desktop opt-in enruta FSM con identidad `uia` fuerte
* approval-v3 requerido
* identidad fuerte y RuntimeId requeridos
* RuntimeId cambiado bloquea
* `InvokePattern` requerido
* rol permitido requerido
* root desktop requerido
* bloqueo FSM no cae a legacy
* no se llama `el.Click`
* no se llama `UiaActionExecutor`
* no se usa `SendInput`
* no se usan coordenadas
* desktop sin `dispatchPath` sigue legacy
* readiness desktop true/false
* default desktop no enruta todavia
* metricas desktop
* invariantes de approval-v2/evidenceHash/policyVersion/ValidateApprovalBinding

## Proximo Hito

H151 - Legacy Deprecation o Desktop Gradual Enablement, segun metricas.
