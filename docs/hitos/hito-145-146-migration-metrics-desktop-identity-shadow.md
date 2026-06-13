# HITO-145/146 — Migration Metrics + Desktop Identity Shadow

## Objetivo

Agregar observabilidad local para decidir la migración gradual de `safe.click` hacia `SafeExecutionFsm` sin cambiar el comportamiento real de ejecución.

- HITO-145 consolida métricas y reportes de readiness.
- HITO-146 agrega un productor desktop read-only de identidad UIA fuerte o débil.

## HITO-145

Se agregó un modelo de resumen local-first:

- `SafeClickMigrationSummary`
- `SafeClickMigrationReadinessReport`
- `SafeClickMigrationReadinessReason`

La evaluación shadow existente de HITO-144 ahora se agrega en un resumen de corrida con:

- total de `safe.click`
- elegibles FSM
- no elegibles FSM
- `approval-v3` fuerte
- `approval-v2`
- identidad weak
- `target.observe` presente/faltante
- `RuntimeId` presente/faltante/stable/changed/unknown
- `InvokePattern` available/unavailable/unknown
- uso legacy de `el.Click`
- uso legacy de `UiaActionExecutor`
- casos que requerirían legacy
- casos que usarían fallback inseguro
- desglose web/desktop

Variables aditivas nuevas:

- `safeClick.migration.total`
- `safeClick.migration.eligibleForFsm`
- `safeClick.migration.notEligibleForFsm`
- `safeClick.migration.blockingReasons`
- `safeClick.migration.readinessPercent`
- `safeClick.migration.legacyFallbackCount`
- `safeClick.migration.summary`
- `safeClick.migration.reportJson`

## HITO-146

Se agregó el step read-only:

- `target.observe.desktop`

Su función es observar un target desktop/UIA sin ejecutar acción y producir identidad reutilizable por:

- `approval.manifest`
- `SafeClickShadowReadiness`
- métricas de migración

Campos observados best-effort:

- `RuntimeId`
- `AutomationId`
- `Name`
- `ControlType`
- `ClassName`
- `FrameworkId`
- `ProcessName`
- `WindowTitle`
- `AncestorPath`
- `BoundingRect` solo como metadata

Salida aditiva típica:

- `{prefix}.identity.runtimeId`
- `{prefix}.identity.automationId`
- `{prefix}.identity.controlType`
- `{prefix}.identity.className`
- `{prefix}.identity.frameworkId`
- `{prefix}.identity.processName`
- `{prefix}.identity.windowTitle`
- `{prefix}.identity.ancestorPath`
- `{prefix}.identity.strength`
- `{prefix}.identity.source`
- `{prefix}.identity.digest`

Clasificación:

- `Strong` si hay `RuntimeId`
- `Weak` si hay criterios estructurales pero falta `RuntimeId`
- `None` si no hay identidad usable

`target.observe.desktop` queda clasificado como `Benign`.

## Por qué todavía no hay flip default

No hay evidencia suficiente para activar rollout por defecto.

Todavía hace falta medir con corpus real:

- estabilidad de `RuntimeId`
- cobertura de `approval-v3` fuerte
- cuántos pasos siguen requiriendo legacy
- cuántos caerían en fallback inseguro
- qué porcentaje del corpus desktop es observable con identidad fuerte

## Qué no cambia

- no flip default
- no ejecución desktop FSM
- no retiro de legacy
- no retiro de `el.Click`
- no retiro de `UiaActionExecutor`
- no `SendInput`
- no coordenadas
- no `GetClickablePoint`
- no OCR
- no `RegionSelector`
- no `BasicActionVerifier`
- no cambios en `approval-v2`
- no cambios en `evidenceHash`
- no cambios en `policyVersion`
- no cambios en `ValidateApprovalBinding`
- no clicks reales

## Qué habilita para el próximo hito

Estos datos dejan preparada la base para:

- rollout gradual por elegibilidad real
- distinción web vs desktop con identidad observada
- decisión informada sobre cuándo mantener legacy y cuándo activar FSM por defecto

## Próximo hito esperado

- `HITO-147 — Gradual Enablement / FSM default for eligible steps`
