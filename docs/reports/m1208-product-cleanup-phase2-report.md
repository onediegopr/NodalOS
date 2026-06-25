# M1208 — Product cleanup phase 2 report

## 1. Decisión

PRODUCT_CLEANUP_PHASE2_DEPRECATED_PROTOCOL_ARTIFACTS_REMOVED_WITH_RESIDUAL_DOC_HISTORY

Se ejecutó limpieza real orientada a producto. No se agregaron features nuevas, gates, claim guards, protocol packs, governance packs ni artifacts safety.

## 2. Qué se borró

Se eliminaron del índice Git los artifacts legacy M933-M1160:

- 247 archivos removidos de `artifacts/agent-operations`.
- 0 artifacts legacy M933-M1160 quedan trackeados.

Familias removidas:

- go/no-go artifacts repetidos.
- claim guards repetidos.
- hold gates.
- operator confirmation artifacts.
- manual QA hold artifacts.
- repeatability/caveat ledgers.
- re-audit package artifacts.
- no-op/metadata/harness protocol artifacts sin conexión con producto visible.

## 3. Qué se archivó

La fase 1 ya había archivado 20 reportes M944-M1160 en:

- `docs/archive/security-protocol-history/reports`
- `docs/archive/security-protocol-history/INDEX.md`

En esta fase no se creó otro archive pack. La historia documental queda disponible ahí, pero fuera del roadmap activo.

## 4. Qué tests se eliminaron o redujeron

Se eliminaron 19 tests documentales que sostenían artifacts/reports legacy:

- `NodalOsSafeNoopMetadataOperatorBridgeM933M944Tests.cs`
- `NodalOsControlledNoopRuntimeAdapterQaPrepM945M956Tests.cs`
- `NodalOsLocalHostVisibleNoopSmokeQaProtocolM957M968Tests.cs`
- `NodalOsLocalHostVisibleNoopSmokeHarnessM969M980Tests.cs`
- `NodalOsClaudeAuditIntakeM981M992Tests.cs`
- `NodalOsAuditFindingsRemediationM993M1004Tests.cs`
- `NodalOsReAuditPackageM1005M1016Tests.cs`
- `NodalOsSafetyFreezeReAuditBlockerRemediationM1017M1028Tests.cs`
- `NodalOsManualQaEvidenceCaptureProtocolM1029M1040Tests.cs`
- `NodalOsManualQaOperatorChecklistGateM1041M1052Tests.cs`
- `NodalOsOperatorConfirmationIntakeDryRunM1053M1064Tests.cs`
- `NodalOsOperatorConfirmationPendingHardeningM1065M1076Tests.cs`
- `NodalOsOperatorConfirmationPendingHardeningR2M1077M1088Tests.cs`
- `NodalOsFinalPreCaptureGateM1089M1100Tests.cs`
- `NodalOsBrowserRuntimeGate9IsolationReviewM1101M1112Tests.cs`
- `NodalOsBrowserRuntimeGate9ContainmentPlanM1113M1124Tests.cs`
- `NodalOsBrowserRuntimeRepeatabilityEvidencePlanM1125M1136Tests.cs`
- `NodalOsBrowserRuntimeRepeatabilityEvidencePlanRevalidationM1137M1148Tests.cs`
- `NodalOsRepeatabilityEvidencePlanHoldM1149M1160Tests.cs`

Resultado:

- `tests/OneBrain.Safety.Tests`: 431 -> 412 archivos trackeados.
- No quedan tests activos referenciando artifacts/reports M933-M1160 por los patrones revisados.

## 5. Qué se mantuvo

Producto visible:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`

Tests útiles:

- `NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- Tests existentes de bridge/extensión/sidepanel no eliminados.

Docs activos:

- `docs/reports/m1172-product-visible-local-demo-v0.md`
- `docs/reports/m1184-product-roadmap-alignment-audit-cleanup-plan.md`
- `docs/reports/m1196-product-cleanup-phase1-report.md`
- `docs/reports/m1208-product-cleanup-phase2-report.md`

## 6. Qué cambió en UX

Mission Control se aplanó para que la demo sea la cara principal:

- La command surface heredada pasó a `Modo avanzado`.
- El texto inicial de instrucción avanzada ahora habla de resumen local de la demo.
- `Smoke caveat` ya no aparece en la UI principal.
- `BrowserRuntimeSmoke caveat visible` ya no aparece en el reporte copiable de la demo.
- `No permitido` pasó a `Límites`.
- `Blocked` pasó a `Revisar`.
- `Blocked by policy` pasó a `Revisar antes de seguir`.

La UI principal queda enfocada en:

- Demo local.
- Run demo.
- Timeline.
- Logs.
- Evidencia.
- Resumen.
- Estado local.
- Sin acciones peligrosas.
- Copiar resumen.

## 7. Qué queda pendiente para fase 3

- Revisar copy técnico restante en flujos avanzados de handoff/consent/recipe/runtime.
- Reducir reports históricos anteriores a M933 si no aportan a producto actual.
- Separar mejor código de Mission Control de helpers heredados en `sidepanel.js`.
- Preparar el siguiente bloque de producto real: mission creation + persistent demo history.

## 8. Qué quedó como delete candidate

- Docs/reports históricos pre-M933 que no conecten con producto visible.
- Artifacts anteriores a M933 que sólo sostengan decisiones antiguas sin valor operativo.
- Tests documentales pre-M933 que validen strings de governance en vez de producto.

No se declara limpieza total: todavía hay residuos históricos fuera de M933-M1160.

## 9. Validaciones

Validaciones ejecutadas:

- `dotnet build .\OneBrain.slnx --no-restore`
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`
- `git diff --check`
- secret scan simple
Resultado:

- Build: PASS con warnings existentes.
- JS syntax: PASS.
- Tests producto visible: PASS, 7/7.
- Diff check: PASS.
- Secret scan simple: PASS, sin matches.

## 10. Riesgos

- Si alguien ejecuta filtros antiguos de M933-M1160 ya no existirán esos tests ni artifacts.
- El archivo histórico en `docs/archive/security-protocol-history` conserva contexto, pero no todos los JSON originales.
- La full suite puede descubrir referencias legacy no detectadas por los patrones de esta limpieza; si ocurre, debe tratarse como cleanup residual, no como motivo para restaurar la burocracia.

## 11. Próximo paso de producto

M1209-M1220 — Product Demo v1: Mission Creation + Persistent Demo History

Objetivo:

- Crear misión desde UI.
- Persistir historial demo local.
- Ver runs anteriores.
- Reabrir y copiar reportes de runs previos.
- Mantener la demo sin provider/cloud y sin runtime real hasta decisión explícita.
