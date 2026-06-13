# HITO-154 — Safe.Click Legacy Retirement

## Objetivo

Retirar el camino ejecutable legacy de `safe.click`.

Desde este hito, `safe.click` sólo puede ejecutar por el path seguro FSM:

- `dispatchPath=safe-executor`
- default eligible-only web/desktop habilitado por kill-switch

El flujo legacy queda bloqueado fail-closed.

## Semántica post-retirement

### `dispatchPath=safe-executor`

Permanece habilitado si cumple:

- approval-v3 strong
- `ApprovalBindingValidator`
- `RecipeSafetyContract`
- `SafeExecutionFsm`
- `UiaPatternExecutor`
- `ExecutorSurfacePolicy`

### sin `dispatchPath`

Depende de `ONEBRAIN_SAFE_CLICK_FSM_DEFAULT`:

- `disabled` / `legacy`: bloquea `SafeClickLegacyRetired`
- `web-eligible`: web eligible enruta FSM; el resto bloquea `SafeClickLegacyRetired`
- `desktop-eligible`: desktop eligible enruta FSM; el resto bloquea `SafeClickLegacyRetired`
- `all-eligible`: web/desktop eligible enrutan FSM; el resto bloquea `SafeClickLegacyRetired`

No hay fallback silencioso.

### `dispatchPath=legacy`

Ya no ejecuta acción.

Bloquea:

```text
LegacyDispatchRetired
```

Se preservan variables de deprecación para evidencia:

- `safeClick.legacy.explicitOptOut`
- `safeClick.legacy.deprecated`
- `safeClick.legacy.deprecationPolicy.*`

## Política de retiro

`SafeClickLegacyRetirementPolicy` define el bloqueo post-retirement.

Campos principales:

- `Enabled`
- `Retired`
- `Blocked`
- `Reason`
- `DispatchPath`
- `RequiredAction`
- `IneligibleAfterRetirement`
- `LegacyDispatchRejected`

Acción requerida:

```text
Use safe-executor/FSM eligible path
```

## Variables nuevas

- `safeClick.legacy.retired`
- `safeClick.legacy.retirementPolicy.enabled`
- `safeClick.legacy.retirementPolicy.reason`
- `safeClick.legacy.retirementPolicy.blocked`
- `safeClick.legacy.retirementPolicy.dispatchPath`
- `safeClick.legacy.retirementPolicy.requiredAction`
- `safeClick.retirement.blockedLegacyExecution`
- `safeClick.retirement.blockReason`
- `safeClick.retirement.ineligibleAfterRetirement`
- `safeClick.retirement.legacyDispatchRejected`

## Métricas nuevas

- `safeClick.migration.legacyRetired`
- `safeClick.migration.legacyExecutionBlocked`
- `safeClick.migration.legacyDispatchRejected`
- `safeClick.migration.ineligibleBlockedAfterRetirement`
- `safeClick.migration.safeExecutorRequired`
- `safeClick.migration.elClickReachableFromSafeClick`
- `safeClick.migration.uiaActionExecutorReachableFromSafeClick`

Los dos últimos deben quedar en `0`.

## Garantías

`safe.click` ya no ejecuta:

- `el.Click`
- `UiaActionExecutor`
- `SendInput`
- coordenadas
- `GetClickablePoint`

`safe.click` no cae a legacy cuando FSM bloquea.

## Qué no se hizo

- No se retiraron globalmente clases legacy que puedan servir a otros flujos.
- No se agregó `safe.type`.
- No se tocaron interfaces.
- No se tocaron APIs externas.
- No se cambió `approval-v2`.
- No se cambió `evidenceHash`.
- No se cambió `policyVersion`.
- No se cambió `ValidateApprovalBinding`.
- No se tocó `RegionSelector`.
- No se tocó `BasicActionVerifier`.

## Próximo hito

HITO-155 — Claude State-of-the-Art Core Engine Audit.
