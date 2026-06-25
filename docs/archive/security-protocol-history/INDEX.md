# Security/protocol history archive

Este archivo conserva la historia de seguridad/protocolo generada durante la línea M933-M1160. No es roadmap activo de producto.

## Por qué se archivó

El trabajo M933-M1160 acumuló reportes de no-op harness, manual QA, operator confirmation, BrowserRuntimeSmoke caveat, repeatability y re-audit. Esa línea fue útil como historial, pero dejó de ser la prioridad.

La prioridad actual es:

1. Producto visible.
2. Funciona.
3. Se puede probar.
4. Limpieza.
5. Seguridad mínima.

## Qué se archivó

Reportes movidos desde `docs/reports` a `docs/archive/security-protocol-history/reports`:

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

## Qué no se archivó

- `docs/reports/m1172-product-visible-local-demo-v0.md`
- `docs/reports/m1184-product-roadmap-alignment-audit-cleanup-plan.md`
- `docs/reports/m1196-product-cleanup-phase1-report.md`

## Artifacts

Los artifacts `artifacts/agent-operations/m933` a `m1160` no se movieron en esta fase porque varios tests existentes referencian paths exactos. Quedan como legacy/deprecated y no deben guiar el roadmap activo.

La fase 2 debe decidir si se archivan o se eliminan junto con los tests documentales que los validan.
