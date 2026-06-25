# M1184 — Product roadmap alignment audit + cleanup plan

## 1. Decisión

PRODUCT_ROADMAP_ALIGNMENT_AUDIT_CLEANUP_PLAN_READY

Este bloque no implementa features nuevas. Audita el estado real del repo después del pivot a producto visible y propone una limpieza concreta para cortar context drift, roadmap drift y scope creep documental.

## 2. Resumen honesto

Sí hubo drift.

La línea M933-M1160 produjo una cantidad alta de documentos, artifacts, matrices, gates y tests que validan burocracia más que producto. El pivot M1161-M1172 sí movió producto visible: tocó el sidepanel real y agregó Mission Control, demo no-op, timeline, evidence panel y reporte copiable.

Señales cuantitativas observadas:

- `docs/reports`: 510 archivos trackeados.
- `artifacts/agent-operations`: 1936 archivos trackeados.
- `tests/OneBrain.Safety.Tests`: 431 archivos trackeados.
- Desde `2ab36b2532996e521e01c9ab53a2837ed6c75456` hasta HEAD:
  - 247 artifacts modificados/agregados.
  - 21 reports modificados/agregados.
  - 20 tests safety modificados/agregados.
  - 3 archivos de producto modificados antes/durante el pivot visible.
- Desde `1827ad732cfa4945d32c5a00dfa5d08f51891e2f` hasta HEAD:
  - 5 archivos cambiados.
  - Los 5 corresponden al bloque de producto visible M1161-M1172.

Conclusión: el repo necesita conservar la demo visible y comprimir/archivar la línea protocolaria para que el próximo trabajo vuelva a producto.

## 3. Qué sí sirve

### KEEP_PRODUCT

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`
- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/tests/fixture_tests.js`

Motivo: son superficie real de producto o soporte directo de la extensión.

### KEEP_MINIMAL_TEST

- `tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`
- Tests existentes de sidepanel/bridge que detectan roturas reales de extensión.
- Tests de build/sintaxis/fixture que protegen la demo visible.

Motivo: validan producto o evitan romper la extensión. No deberían crecer hacia más matrices de gobernanza.

### KEEP_DOC

- `docs/reports/m1172-product-visible-local-demo-v0.md`
- `browser-extension/onebrain-chrome-lab/README.md`
- Un futuro documento corto tipo `docs/product-demo.md` o `docs/reports/product-roadmap-current.md`.

Motivo: documentación operable, corta y útil para abrir/probar el producto.

## 4. Qué parece drift

### ARCHIVE_SECURITY_DOC

Archivar fuera del camino principal:

- `docs/reports/m944-safe-noop-metadata-runner-local-operator-evidence-bridge.md`
- `docs/reports/m956-controlled-noop-runtime-adapter-local-operator-qa-prep.md`
- `docs/reports/m968-local-host-visible-noop-smoke-manual-qa-evidence-protocol.md`
- `docs/reports/m980-local-host-visible-noop-smoke-harness-evidence-gate.md`
- `docs/reports/m992-claude-audit-intake-harness-prep.md`
- `docs/reports/m1004-audit-findings-remediation.md`
- `docs/reports/m1016-re-audit-package-remediated-noop-harness-safety.md`
- `docs/reports/m1028-safety-freeze-re-audit-blocker-remediation.md`
- `docs/reports/m1040-manual-qa-evidence-capture-protocol-execution-prep.md`
- `docs/reports/m1052-manual-qa-operator-checklist-gate-protocol-hardening.md`
- `docs/reports/m1064-operator-confirmation-intake-safe-evidence-dry-run-packet.md`
- `docs/reports/m1076-operator-confirmation-pending-protocol-hardening-browserruntime-caveat-review.md`
- `docs/reports/m1088-operator-confirmation-pending-hardening-r2-caveat-evidence-delta.md`
- `docs/reports/m1100-final-pre-capture-gate-caveat-resolution-criteria-audit.md`
- `docs/reports/m1112-browserruntime-gate9-isolation-review.md`
- `docs/reports/m1124-browserruntime-gate9-containment-remediation-plan.md`
- `docs/reports/m1136-browserruntime-repeatability-evidence-plan-prep.md`
- `docs/reports/m1148-browserruntime-repeatability-evidence-plan-revalidation.md`
- `docs/reports/m1160-repeatability-evidence-plan-hold-execution-request-gate.md`

Estos documentos pueden quedar como historia/audit, pero no deberían aparecer como roadmap activo.

### DELETE_CANDIDATE

Después de archivar o condensar en un resumen histórico, son candidatos a borrar:

- `artifacts/agent-operations/m933` a `artifacts/agent-operations/m1160` cuando sean JSON de matrices, go/no-go, gates, claim guards, hold ledgers o final reports duplicados.
- Carpetas agregadas sólo para justificar otra ronda de protocol hardening.
- Artifacts cuyo contenido no conecta con código de producto, UI, bridge real o tests útiles.
- Duplicados de BrowserRuntimeSmoke caveat, repeatability, operator confirmation y manual QA hold.

No borrar en este bloque. Primero crear un índice de archive y ejecutar una PR de cleanup dedicada.

## 5. Qué está inflado

La mayor inflación está en cuatro familias:

1. Operator confirmation / manual QA gates.
2. BrowserRuntimeSmoke caveat / repeatability / false-clean guards.
3. No-op harness / metadata / evidence contracts.
4. Re-audit packages y re-audit followups.

Ejemplo desde M933 hasta HEAD:

- 90 artifacts con patrones `gate`, `go-no-go`, `guard`, `matrix`, `hold`, `freeze`, `claim` o `abort`.
- 52 artifacts relacionados con manual QA/operator/evidence capture.
- 32 artifacts relacionados con caveat/BrowserRuntimeSmoke/Gate 9/repeatability.
- 29 artifacts relacionados con no-op/metadata/adapter/harness.

Estos grupos tuvieron valor como guardrail temporal, pero ahora bloquean el foco de producto.

## 6. Auditoría de producto visible

Archivos revisados:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`

### Qué se ve

- Mission Control visible dentro de `Operar`.
- Sidebar mínima.
- Hero con `NODAL OS` y `Local Operator Demo`.
- Botón `Run safe demo`.
- Botón `Copiar resumen`.
- Progreso.
- Timeline central.
- Rail de estado local/bridge/browser claim/caveat.
- Logs/evidence.
- Reporte técnico copiable.

### Cumplimiento Mission Control básico

Cumple como demo v0. Ya hay algo visible, clickeable y entendible.

### Fricción actual

La demo nueva tiene microcopy razonable, pero el resto del sidepanel arrastra términos heredados:

- `blocked`
- `Blocked by policy`
- `No permitido`
- `Core authority required`
- `ReadyWithRestrictions`
- `BrowserRuntimeSmoke caveat visible`
- referencias a sensitive sites, credentials y policy en timeline genérico.

Parte de esto puede seguir existiendo internamente, pero no debe dominar la UX principal.

### Botones relevantes

- `Run safe demo`
- `Copiar resumen`
- Botones heredados: `Ejecutar`, `Pausar`, `Reanudar`, `STOP`, runtime/recipe/learning.

### Qué falta para usarlo mejor

- Crear misión desde UI.
- Persistir runs demo.
- Ver historial.
- Reabrir run anterior.
- Limpiar la superficie `Operar` para que Mission Control sea el producto principal y el command textarea quede como modo avanzado.

### Evolución posible

Sí puede evolucionar a misión/historial. La estructura de `state.demo`, timeline y report ya es suficiente para M1173-M1184.

## 7. Qué conviene mantener

Mantener:

- Product files de extensión.
- Demo visible M1161-M1172.
- Tests de producto visible.
- Tests de bridge/sidepanel que detectan roturas reales.
- Un resumen histórico de seguridad, no cientos de artifacts activos.
- BrowserRuntimeSmoke como caveat técnico breve en reporte interno, no como eje del roadmap.

## 8. Qué conviene reescribir

### REWORK

- `sidepanel.js`: separar demo/product state de helpers policy-heavy heredados.
- `sidepanel.html`: mover command surface heredada debajo de un bloque “Avanzado”.
- `sidepanel.css`: mantener dark Mission Control, eliminar tokens/comentarios de “Research OS” si ya no representan el producto.
- Timeline genérico: cambiar copy de `Blocked/No permitido/Core authority` por estados de producto más humanos.
- Reporte técnico copiable: mantenerlo, pero hacerlo más útil para demo y menos caveat-driven.

## 9. Qué conviene borrar/archivar

### Archivar primero

Crear una carpeta o rama de archivo, por ejemplo:

- `docs/archive/security-protocol-history/`
- `artifacts/archive/agent-operations-security-history/`

Mover allí la línea M933-M1160 que no conecta con producto visible.

### Borrar después

Sólo después de validar que no hay tests críticos referenciando esos artifacts:

- Go/no-go JSON repetidos.
- Final reports JSON repetidos.
- Claim guards duplicados.
- Operator confirmation matrices.
- Manual QA hold matrices.
- Repeatability/caveat ledgers duplicados.
- Tests que sólo verifican que esos JSON existen y contienen strings burocráticos.

## 10. Riesgos de revertir a ciegas

- Se puede romper un test suite que todavía espera artifacts antiguos.
- Se puede borrar evidencia histórica útil para auditoría.
- Se puede perder contexto de por qué no se desbloqueó runtime real.
- Se puede tocar accidentalmente código de bridge/extension útil si se usa un patrón de borrado amplio.
- Se puede confundir cleanup documental con cambio de seguridad real.

No usar `git reset` ni borrados masivos. Cleanup debe ser por lista y commit pequeño.

## 11. Plan de cleanup por fases

### Fase 1 — limpieza documental segura

- Crear un índice histórico único para M933-M1160.
- Mover reports security/protocol a archivo.
- Mover artifacts M933-M1160 a archivo o eliminarlos si el usuario aprueba.
- Mantener `m1172-product-visible-local-demo-v0.md` como documento activo de producto.

### Fase 2 — limpieza UI/copy

- Hacer Mission Control la primera experiencia.
- Mover command textarea heredado a modo avanzado.
- Bajar caveat a badge técnico discreto o sacarlo de UI principal.
- Reemplazar `NO-GO`, `blocked`, `claim guard`, `policy` por copy simple:
  - `Demo segura`
  - `Listo para probar`
  - `Sin acciones peligrosas`
  - `Copiar resumen`
  - `Modo avanzado`

### Fase 3 — tests útiles

- Mantener tests que verifican:
  - sidepanel carga.
  - JS sintácticamente válido.
  - demo visible existe.
  - `Run safe demo` no llama runtime/bridge.
  - no secrets.
- Archivar o borrar tests que sólo validan artifacts/doc strings.
- Evitar nuevas categorías de tests por cada micro-hito documental.

### Fase 4 — volver a producto

- Mission creation.
- Persistent demo history.
- Run history.
- Evidence visible simple.
- Demo grabable.

## 12. Próximo bloque recomendado

M1185-M1196 — Product Cleanup Phase 1: Archive Security Protocol History + UI Copy Simplification Plan

Alcance recomendado:

- No features nuevas.
- No borrado masivo.
- Crear índice histórico único.
- Mover/archivar docs security/protocol seleccionados.
- Preparar lista exacta de tests/artifacts a borrar en una fase posterior.
- Reducir copy visible en Mission Control sin tocar runtime real.

## Clasificación resumida

| Clasificación | Archivos/familias |
| --- | --- |
| KEEP_PRODUCT | `browser-extension/onebrain-chrome-lab/*` productivos, especialmente `sidepanel.html/css/js` |
| KEEP_MINIMAL_TEST | `NodalOsProductVisibleLocalDemoM1161M1172Tests.cs`, tests sidepanel/bridge reales |
| KEEP_DOC | `m1172-product-visible-local-demo-v0.md`, README de extensión, futuro roadmap corto |
| ARCHIVE_SECURITY_DOC | Reports M944-M1160 excepto M1172 |
| DELETE_CANDIDATE | Artifacts M933-M1160 repetidos de gates, matrices, go/no-go, claim guards |
| REWORK | Copy y layout heredado del sidepanel, tests que validan burocracia |
| UNKNOWN_USER_DECISION | Docs/tests históricos pre-M900 no relacionados con el pivot actual |
