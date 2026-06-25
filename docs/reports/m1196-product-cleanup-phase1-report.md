# M1196 — Product cleanup phase 1 report

## 1. Decisión

PRODUCT_CLEANUP_PHASE1_ARCHIVE_READY_WITH_DELETE_CANDIDATES

Se ejecutó limpieza fase 1 sin features nuevas. El producto visible se mantiene, la historia security/protocol M933-M1160 se archivó en docs, y la UI principal redujo wording burocrático visible.

## 2. Qué se limpió

- Se movieron reportes security/protocol M944-M1160 fuera de `docs/reports`.
- Se creó un índice histórico único en `docs/archive/security-protocol-history/INDEX.md`.
- Se simplificó el copy visible del Mission Control para que no use caveat/smoke wording en la UI principal.
- Se actualizó el test de producto visible para proteger la copy simplificada.

## 3. Qué se archivó

Reportes archivados en `docs/archive/security-protocol-history/reports`:

- `m944-safe-noop-metadata-runner-local-operator-evidence-bridge.md`
- `m956-controlled-noop-runtime-adapter-local-operator-qa-prep.md`
- `m968-local-host-visible-noop-smoke-manual-qa-evidence-protocol.md`
- `m980-local-host-visible-noop-smoke-harness-evidence-gate.md`
- `m984-claude-deep-audit-prompt.md`
- `m992-claude-audit-intake-harness-prep.md`
- `m1004-audit-findings-remediation.md`
- `m1016-re-audit-package-remediated-noop-harness-safety.md`
- `m1028-safety-freeze-re-audit-blocker-remediation.md`
- `m1040-manual-qa-evidence-capture-protocol-execution-prep.md`
- `m1052-manual-qa-operator-checklist-gate-protocol-hardening.md`
- `m1064-operator-confirmation-intake-safe-evidence-dry-run-packet.md`
- `m1076-operator-confirmation-pending-protocol-hardening-browserruntime-caveat-review.md`
- `m1088-operator-confirmation-pending-hardening-r2-caveat-evidence-delta.md`
- `m1100-final-pre-capture-gate-caveat-resolution-criteria-audit.md`
- `m1112-browserruntime-gate9-isolation-review.md`
- `m1124-browserruntime-gate9-containment-remediation-plan.md`
- `m1136-browserruntime-repeatability-evidence-plan-prep.md`
- `m1148-browserruntime-repeatability-evidence-plan-revalidation.md`
- `m1160-repeatability-evidence-plan-hold-execution-request-gate.md`

## 4. Qué quedó activo

Docs activos de producto/roadmap:

- `docs/reports/m1172-product-visible-local-demo-v0.md`
- `docs/reports/m1184-product-roadmap-alignment-audit-cleanup-plan.md`
- `docs/reports/m1196-product-cleanup-phase1-report.md`

Producto visible:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`

Tests útiles:

- `tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- Tests de bridge/sidepanel que protegen extensión real.

## 5. Qué se mantuvo

- Mission Control visible.
- Misión `Local Operator Demo`.
- Botón `Run safe demo`.
- Timeline visual.
- Panel logs/evidencia.
- Reporte técnico copiable.
- Build y tests de demo visible.

## 6. Qué queda como delete candidate

Artifacts legacy/deprecated:

- `artifacts/agent-operations/m933` a `artifacts/agent-operations/m1160`.

Familias candidatas:

- Go/no-go JSON repetidos.
- Claim guards.
- Hold gates.
- Manual QA/operator confirmation matrices.
- BrowserRuntimeSmoke repeatability/caveat ledgers.
- Re-audit package artifacts.
- Harness/no-op protocol artifacts que no conectan con UI o runtime real.

No se movieron en esta fase porque hay tests con paths exactos. Borrarlos sin fase 2 rompería la suite documental.

## 7. Qué tests conviene reducir en fase 2

Candidatos por validar documentos/artifacts en vez de producto:

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

Mantener:

- `NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- Tests de bridge/extension reales.
- Tests que validan sintaxis, manifest, sidepanel, service worker y recipe runner.

## 8. Qué cambió en UI/copy

Antes:

- `Smoke caveat`
- `BrowserRuntimeSmoke caveat visible`
- `badge informativo, no bloquea esta demo`

Ahora:

- `Demo scope`
- `No-op local`
- `sin shell, filesystem ni cloud`
- Reporte copiable con `scope` y `mode`, no con caveat como mensaje principal.

La UI sigue mostrando guardrail mínimo como lenguaje de producto: “sin acciones peligrosas”, no como burocracia de auditoría.

## 9. Validaciones

Ejecutadas:

- `dotnet build .\OneBrain.slnx --no-restore`
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=M1161M1172"`
- `git diff --check`
- secret scan simple
- `git status`

Resultado:

- Build: PASS con warnings existentes.
- JS syntax: PASS.
- Tests producto visible: PASS.
- Diff check: PASS.
- Secret scan: PASS.

## 10. Riesgos

- Full suite documental puede fallar si espera reportes en `docs/reports`.
- Artifacts legacy siguen ocupando espacio hasta fase 2.
- Tests burocráticos siguen existiendo hasta limpieza de fase 2.
- Algunas funciones heredadas del sidepanel todavía usan copy técnico en flujos avanzados.

## 11. Próximo paso de producto

M1197-M1208 — Product Cleanup Phase 2: Delete Deprecated Artifact Tests + Mission Control UX Flattening

Objetivo:

- Quitar o reescribir tests que bloquean la eliminación de artifacts legacy.
- Borrar artifacts M933-M1160 que no conectan con producto.
- Dejar Mission Control como experiencia principal.
- Mover superficie de comandos heredada a modo avanzado.
- Preparar M1209+ para Mission Creation + Persistent Demo History.
