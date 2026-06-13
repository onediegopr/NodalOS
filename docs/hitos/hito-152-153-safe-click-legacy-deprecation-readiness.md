# HITO-152/153 - Safe.Click Legacy Deprecation + Retirement Readiness

## Objetivo

HITO-152 formaliza la deprecacion de legacy para `safe.click`.

HITO-153 agrega un gate objetivo de readiness para decidir cuando retirar legacy fisicamente.

Este hito no retira legacy, no retira `el.Click`, no retira `UiaActionExecutor` y no cambia el comportamiento existente de `safe.click`.

## H152 - Legacy Deprecation

`dispatchPath=legacy` sigue funcionando, pero ahora queda marcado como deuda explicita.

Metadata canonica del step:

```json
{
  "kind": "safe.click",
  "dispatchPath": "legacy",
  "legacyOwner": "migration",
  "legacyReason": "Target lacks InvokePattern",
  "legacyReviewBy": "2026-07-31"
}
```

Tambien se aceptan alias menores:

* `owner`
* `reason`
* `reviewBy`

Para H152:

* no se bloquea legacy por falta de metadata
* se marca `Warning`
* se exponen variables aditivas
* se incrementan metricas de opt-out no compliant
* se documenta que un hito futuro puede bloquear

## SafeClickLegacyDeprecationPolicy

Modelo core puro:

* `IsLegacyDispatch`
* `IsDeprecated`
* `RequiresOwner`
* `RequiresReason`
* `RequiresReviewBy`
* `Owner`
* `Reason`
* `ReviewBy`
* `DeprecationSeverity`
* `IsCompliant`
* `ViolationReason`

Severidades:

* `None`
* `Warning`
* `ErrorFuture`
* `BlockedFuture`

H152 usa `Warning`.

## Variables de Deprecacion

Variables aditivas:

* `safeClick.legacy.deprecationPolicy.enabled`
* `safeClick.legacy.deprecationPolicy.isLegacyDispatch`
* `safeClick.legacy.deprecationPolicy.isDeprecated`
* `safeClick.legacy.deprecationPolicy.owner`
* `safeClick.legacy.deprecationPolicy.reason`
* `safeClick.legacy.deprecationPolicy.reviewBy`
* `safeClick.legacy.deprecationPolicy.isCompliant`
* `safeClick.legacy.deprecationPolicy.violationReason`
* `safeClick.legacy.deprecationPolicy.severity`

Variables existentes conservadas:

* `safeClick.legacy.explicitOptOut`
* `safeClick.legacy.deprecated`
* `safeClick.legacy.reason`

## Metricas de Deprecacion

Metricas locales por corrida:

* `safeClick.migration.legacyExplicitOptOutTotal`
* `safeClick.migration.legacyOptOutCompliant`
* `safeClick.migration.legacyOptOutMissingOwner`
* `safeClick.migration.legacyOptOutMissingReason`
* `safeClick.migration.legacyOptOutMissingReviewBy`
* `safeClick.migration.legacyOptOutNonCompliant`
* `safeClick.migration.legacyDeprecationWarnings`

No hay telemetria externa ni persistencia compleja.

## H153 - Retirement Readiness Gate

`SafeClickLegacyRetirementReadiness` calcula si una corrida local esta lista para retirar legacy.

No representa historia real ni ventana temporal persistida.

Campos:

* `TotalSafeClicks`
* `DefaultFsmRouted`
* `ExplicitLegacyOptOut`
* `LegacyPathUsed`
* `ElClickUsed`
* `UiaActionExecutorUsed`
* `UnsafeFallbackUsed`
* `NonCompliantLegacyOptOut`
* `DesktopExcluded`
* `WebExcluded`
* `AllEligibleModeObserved`
* `IsReadyForRetirement`
* `BlockingReasons`

## Criterio de Readiness

`IsReadyForRetirement == true` solo si:

* no hubo legacy default
* no hubo `el.Click` default
* no hubo `UiaActionExecutor` default
* no hubo unsafe fallback
* no hubo opt-out legacy no compliant
* no hubo dispatchPath desconocido
* todos los `safe.click` activos fueron FSM elegibles/ruteados o `dispatchPath=legacy` explicitamente compliant

## Variables de Retirement

Variables aditivas:

* `safeClick.retirement.ready`
* `safeClick.retirement.blockingReasons`
* `safeClick.retirement.legacyPathUsed`
* `safeClick.retirement.elClickUsed`
* `safeClick.retirement.uiaActionExecutorUsed`
* `safeClick.retirement.unsafeFallbackUsed`
* `safeClick.retirement.nonCompliantLegacyOptOut`
* `safeClick.retirement.summary`
* `safeClick.retirement.reportJson`

Metricas:

* `safeClick.migration.retirementReady`
* `safeClick.migration.retirementBlocked`
* `safeClick.migration.retirementBlockingReasons`
* `safeClick.migration.defaultLegacyUse`
* `safeClick.migration.defaultElClickUse`
* `safeClick.migration.defaultUiaActionExecutorUse`
* `safeClick.migration.nonCompliantLegacyOptOut`

## Que No Cambia

* no retiro legacy todavia
* no retiro de `el.Click`
* no retiro de `UiaActionExecutor`
* legacy sigue funcionando
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

* legacy dispatch path sigue funcionando
* legacy dispatch path marca deprecated
* metadata completa produce opt-out compliant
* faltan owner/reason/reviewBy produce warning no compliant
* deprecacion no bloquea todavia
* variables y metricas de deprecacion
* retirement readiness false con legacy default
* retirement readiness false con `el.Click`, `UiaActionExecutor` o unsafe fallback
* retirement readiness false con opt-out no compliant
* retirement readiness true sin blocking reasons
* reporte deterministico
* invariantes de approval-v2/evidenceHash/policyVersion/ValidateApprovalBinding

## Proximo Hito

H154 - Safe.Click Legacy Retirement.

H155 - Auditoria integral Claude del nucleo.
