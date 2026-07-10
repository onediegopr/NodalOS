# NODAL OS — Estado Canónico Reconciliado del Roadmap

Fecha: 2026-07-10

HEAD base de esta reconciliación: `d3b7cfc2e587aaf38870856cf04074a3f730bf2d`

Decisión:

`GO_WITH_FINDINGS_CANONICAL_ROADMAP_RECONCILED_MVP_VERTICAL_SLICE_NEXT`

Este documento corrige y reemplaza, para planificación futura, porcentajes históricos que mezclaban documentación, readiness local y producto vendible.

## 1. Cambios ya absorbidos después de la auditoría técnica inicial

El estado actual ya contiene remediaciones que no estaban presentes en el baseline `669cd914`:

- `ChromeLabBridgeSecurity.cs` con política central de seguridad del bridge.
- Hardening del estado de autenticación en `ExtensionMessageHandler` y `WebSocketSession`.
- Cliente de auth de extensión en `bridge_auth.js`.
- Tests focales de seguridad del bridge.
- Proyecto focal `OneBrain.ChromeLab.Tests` agregado a la solución.
- Workflow `tier1-safety.yml`.
- Workflow de limpieza de artefactos con referencias.
- CODEOWNERS y plantilla de PR.
- Documentación de branch governance y deployments.
- Limpieza amplia de artefactos históricos transitorios.

Estado de estas remediaciones:

`LOCAL_VALIDATION_PASS_REMOTE_CI_AND_BRANCH_PROTECTION_PENDING`

No deben seguir tratándose como trabajo ausente. El cierre local ejecutado valida el bridge como lab seguro, pero no equivale a CI remoto obligatorio, branch protection ni cierre productivo.

## 2. Estado ejecutivo real

- Cobertura de roadmap/governance: `93%`.
- Implementación técnica agregada: `70%`.
- Integración de producto: `52%`.
- MVP vendible: `49%`.
- Production runtime: `0%`.
- Release/commercial: `0% / NO-GO`.

La cifra histórica `98%` queda retirada como indicador de producto. Sólo describía amplitud de roadmap y líneas de preparación.

## 3. Matriz completa actual

| Área | Realizado | Pendiente | Estado | % |
|---|---|---|---|---:|
| Roadmap/governance | Roadmaps, logs, handoffs, audits, CODEOWNERS, PR template | Consolidar un solo source of truth y retirar porcentajes viejos | Avanzado | 93% |
| Arquitectura core | Solución modular, contratos, core, safety, verification | Reducir duplicaciones y escoger caminos canónicos | Avanzado/parcial | 82% |
| Safety/guardrails | Fail-closed, path jail, redaction, consent, approval guards | Ejecutar Tier 1 continuo y cerrar adversarial gaps | Avanzado | 86% |
| ChromeLab security | Política central, WS auth hardening, auth client, tests, smoke runtime local | CI remoto y branch protection quedan separados por acceso GitHub | Local validation PASS / lab seguro | 84% |
| Branch/CI governance | Workflows Tier 1 y cleanup, CODEOWNERS, docs | Cambiar default branch, protección obligatoria, demostrar runs remotos PASS | Inicial funcional | 38% |
| Artifact hygiene | Gran limpieza y tooling reference-aware | Segunda pasada y verificación de links/fixtures | Avanzado | 78% |
| Runtime local | Varios runtimes y surfaces locales | Unificar en vertical slice | Parcial | 61% |
| Approval/Evidence/Timeline | Contratos, previews, ledger, durable evidence | Loop de producto integrado y minimal evidence schema | Parcial/avanzado | 69% |
| Mission Control UX | Sidepanel/Research OS, timeline, workspace views | Una UI canónica conectada a runtime real | Parcial | 55% |
| Workspace/Understanding | Models, consent, jail, fixture/dry-run | Scan real bounded, index local, performance | Parcial | 49% |
| Assignment/Planning | TaskGraph, plans y previews | Persistencia y ejecución aprobada | Parcial | 53% |
| BYOK/LLM | OpenAI bridge y diseños de provider/budget | Provider canónico, secure store, budget enforcement | Parcial | 36% |
| CloakBrowser direct CDP | Fundaciones browser/CDP y decisión permanente | Adapter productivo canónico e integración | Temprano/parcial | 38% |
| Chrome extension | Sidepanel, bridge, recipes, learning, handoff | Mantener lab/legacy y migrar capacidades necesarias | Lab avanzado | 58% |
| Windows Computer Use | UIA/OCR/context/fusion/guards | Productización y QA real | Parcial | 44% |
| Product Ledger | Local evidence y operator surfaces | Integrarlo sin convertirlo en producto aparte | Parcial/avanzado | 67% |
| Export/Handoff | Contratos y controlled export lines | Export usable por usuario y privacy QA | Parcial | 42% |
| Installer/Updater | Diseño y algunos componentes de distribución | Installer, firma, updater, rollback | Inicial | 14% |
| Cloud/Licensing | Sólo estrategia/targets lab | Auth, licensing, billing, updates service | Pendiente | 5% |
| Production | Sin autoridad | Todo el cierre productivo | NO-GO | 0% |
| Commercial release | Sin release | Beta, support, pricing delivery, operations | NO-GO | 0% |

## 4. Auditoría hacia atrás por líneas

### Cerradas y no reabrir por inercia

- CBPR fixture-safe 001–010.
- Controlled Execution Design Track read-only.
- Local/dev runtime readiness packets ya cubiertos.
- Product Ledger local/dev surface prep.
- ChromeLab operator surface prep.
- Microcopy, guard-only y evidence-wrapper iterations.

Sólo reabrir si aparece un defecto real, una integración del vertical slice o una validación de release.

### Realizadas pero todavía no equivalen a producto

- Research OS y previews visuales.
- Project Understanding sintético/dry-run.
- Assignment planner previews.
- Expert Advisor design.
- Product Ledger local evidence.
- WCU design/prototypes.
- ChromeLab local operator surfaces.
- CI workflows aún sin prueba operativa suficiente.

### Realizadas y útiles como cimiento de producto

- Core contracts.
- Policy/approval boundaries.
- Evidence/timeline foundations.
- Path jail y consent gates.
- Browser/CDP contracts.
- Session API read-only.
- ChromeLab auth hardening implementado.
- Artifact cleanup tooling.
- Tier 1 workflow foundation.

## 5. Contradicciones resueltas

### ChromeLab versus CloakBrowser

- ChromeLab/extension se clasifica `LAB_LEGACY_TRANSITION`.
- CloakBrowser/CDP directo permanece como runtime canónico de producto.
- No se ampliará la extensión como source of truth ni runtime final.

### 98% versus producto vendible

- `93%` roadmap/governance.
- `48%` MVP vendible.
- `0%` production/release.

### Evidence versus valor

- No más wrappers de acceptance equivalentes.
- La evidencia será mínima, canónica y vinculada a una acción real.

### Guards versus avance

- No se aceptan nuevos bloques guard-only salvo riesgo material demostrado.
- Cada macro debe cerrar seguridad, integración, UX o distribución.

## 6. Pendientes reales priorizados

### P0 — Cierre inmediato

1. Cambiar la rama default y aplicar branch protection cuando haya credenciales GitHub.
2. Demostrar run remoto de `tier1-safety.yml` como required check.
3. Mantener LAN disabled y ChromeLab clasificado como `LAB_LEGACY_TRANSITION`.

Macro 1 local ya quedó validado en Codex: ChromeLab focused Release PASS `25/25`, Safety focal Release PASS `35/35`, bridge Release build PASS, Safety aggregate Release build PASS con `32` warnings preexistentes, smoke runtime local PASS para auth/origin/pairing/stealth/no-store y extensión estática PASS.

### P1 — MVP vertical slice

1. Congelar flujo canónico:
   `Workspace → Mission → Plan → Approval → Controlled Action → Verification → Evidence/Timeline → Handoff`.
2. Seleccionar un único modelo por etapa.
3. Implementar adapter CloakBrowser/CDP directo.
4. Conectar Mission Control a estados reales.
5. Implementar una acción útil, acotada y reversible.
6. Completar secure BYOK mínimo.
7. Completar export/handoff usable.

### P2 — Beta privada

1. Project Understanding real bounded.
2. Persistence migrations y recovery.
3. Installer, updater y versioning.
4. Support bundle sanitizado.
5. Matriz Windows/CloakBrowser.
6. E2E smoke reproducible.
7. Privacy/security docs.

### P3 — Después del MVP

- Multi-provider amplio.
- Advisor avanzado.
- WCU amplio.
- LAN/team/cloud sync.
- KMS/WORM.
- Managed AI y enterprise controls.

## 7. Secuencia canónica desde ahora

### Macro 1

`AUTHORIZE_NODAL_OS_CHROMELAB_SECURITY_CI_RUNTIME_VALIDATION_AND_LAB_CLOSEOUT`

Cierre esperado:

- Tests focales y Tier 1 PASS.
- HTTP/WS/stealth auth adversarial PASS.
- Pairing/CORS/LAN boundaries PASS.
- ChromeLab queda cerrado como lab seguro.

### Macro 2

`AUTHORIZE_NODAL_OS_CANONICAL_DEFAULT_BRANCH_PROTECTION_AND_MVP_VERTICAL_SLICE_FREEZE`

Cierre esperado:

- Default branch correcta.
- Protección y required checks.
- Vertical slice y DoD congelados.

### Macro 3

`AUTHORIZE_NODAL_OS_CLOAKBROWSER_DIRECT_CDP_MVP_VERTICAL_SLICE_IMPLEMENTATION`

Cierre esperado:

- CloakBrowser pinneado.
- CDP directo.
- Read-only capture primero.
- Una acción aprobada y verificada después.

### Macro 4

`AUTHORIZE_NODAL_OS_MISSION_CONTROL_WORKSPACE_BYOK_INTEGRATED_PRODUCT_LOOP`

### Macro 5

`AUTHORIZE_NODAL_OS_INSTALLER_UPDATER_EXPORT_AND_PRIVATE_BETA_READINESS`

## 8. Regla de aceptación de nuevos bloques

Aceptar sólo si el bloque:

- Cierra un P0/P1 real.
- Integra una etapa del vertical slice.
- Cambia comportamiento visible.
- Reduce duplicación material.
- Agrega validación necesaria para beta/release.

Rechazar:

- Copy-only.
- Guard-only sin riesgo material.
- Evidence wrapper nuevo.
- Readiness packet equivalente.
- Artifact de micro-hito no consumido.
- Refactor amplio sin integración.
- Expansión de Chrome extension como runtime final.

## 9. Próxima decisión

`READY_FOR_CANONICAL_DEFAULT_BRANCH_PROTECTION_AND_MVP_VERTICAL_SLICE_FREEZE`
