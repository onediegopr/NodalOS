# NODAL OS — Reconciliación Minuciosa del Roadmap Hacia Atrás y Hacia Adelante

Fecha: 2026-07-10

Baseline auditado: `669cd914de3fa1ad24f9fe2bb3269cd5107be1ed`

Rama: `chrome-lab-001-extension-local-ai-bridge`

## 1. Decisión ejecutiva

Estado reconciliado:

`GO_WITH_FINDINGS_ROADMAP_BACKWARD_RECONCILIATION_COMPLETE_PRODUCT_ALIGNMENT_RESET_REQUIRED`

Conclusión principal:

- NODAL OS tiene una base técnica, de seguridad, contratos, evidencia y prototipos locales mucho más avanzada que un proyecto inicial.
- El proyecto no está al 98% de un MVP vendible. El `98%` histórico describe principalmente cobertura de roadmap, decisiones, guardrails y líneas de preparación; no describe producto integrado, instalable, seguro y comercializable.
- El avance real hacia un MVP vendible se estima en `46%`.
- El bloqueo P0 actual es la autenticación uniforme del bridge ChromeLab y sus superficies HTTP/WebSocket.
- La decisión arquitectónica permanente sigue siendo CloakBrowser como runtime de producto. ChromeLab/extension debe permanecer como laboratorio, compatibilidad o transición; no debe convertirse en runtime canónico de producto.
- El siguiente ciclo debe dejar de crear nuevas capas de evidence/readiness y concentrarse en seguridad, integración vertical, UX operativa, packaging y release reproducible.

## 2. Source of truth reconciliado

Orden de autoridad recomendado a partir de este documento:

1. Decisiones permanentes de arquitectura y seguridad.
2. Código y tests realmente ejecutables.
3. Roadmap actual reconciliado.
4. Decision log e handoffs recientes.
5. Artefactos históricos sólo como evidencia, nunca como estado actual.

Reglas permanentes que prevalecen:

- CloakBrowser es el runtime browser único de producto.
- No usar Chrome/Edge del sistema ni Playwright Chromium default como runtime canónico.
- La extensión Chrome es legacy/lab/compatibilidad y debe migrar a CDP directo.
- Stealth Core Auditado es protected scope y no se modifica sin frontera explícita.
- Acciones sensibles requieren explicación, límites, approval, evidence y verificación.
- Local-first y fail-closed por defecto.
- No public/product authority, release o commercial claim sin seguridad, CI, packaging y QA real.

## 3. Distinción obligatoria de porcentajes

Los porcentajes anteriores mezclaban conceptos diferentes. Desde ahora se separan:

- `Roadmap/Governance Coverage`: cuánto del mapa, contratos, guardrails y decisiones está definido.
- `Implementation Readiness`: cuánto código integrado y verificable existe.
- `MVP Sellable Readiness`: cuánto falta para instalar, operar, validar y vender el producto.
- `Production Readiness`: seguridad, observabilidad, distribución, actualización y soporte productivo.

Estado reconciliado:

| Dimensión | Estado real | % reconciliado |
|---|---|---:|
| Roadmap y governance | Muy avanzado, con exceso histórico de micro-hitos | 92% |
| Arquitectura y contratos core | Avanzado, parcialmente fragmentado | 82% |
| Guardrails y safety local | Avanzado, sin cierre productivo | 84% |
| Runtime local integrado | Parcial, múltiples líneas no unificadas | 61% |
| Approval, evidence y timeline | Parcial/avanzado, falta loop final de producto | 69% |
| Mission Control y UX operativa | Parcial, superficies y previews dispersos | 55% |
| Workspace y Project Understanding | Parcial, gran parte fixture/dry-run/read-only | 49% |
| Assignment/Planning | Parcial, modelos y previews más que ejecución integrada | 53% |
| LLM/BYOK/provider governance | Parcial y específico de bridge | 36% |
| Browser product runtime CloakBrowser/CDP directo | Fundaciones/lab; integración producto incompleta | 38% |
| ChromeLab local/dev surface | Útil como lab; no runtime canónico | 48% |
| Windows Computer Use | Diseñado/prototipado con guards; no productizado | 44% |
| Product Ledger local/dev | Fuerte como evidencia local; sin autoridad productiva | 67% |
| Export/handoff usable | Parcial; real download/product flow incompleto | 42% |
| Installer/updater/distribución | Insuficiente | 14% |
| CI y branch governance | Ausente | 0% |
| Cloud/licensing/billing | No implementado como producto | 5% |
| Production runtime | No autorizado/no listo | 0% |
| Release/commercial | NO-GO | 0% |
| MVP vendible total | Base fuerte, integración y distribución incompletas | 46% |

## 4. Auditoría cronológica hacia atrás

### Etapa A — Fundación OneBrain/NODAL OS y Windows/UIA

Objetivo original:

- Crear núcleo local tipado.
- Observar Windows y UIA.
- Derivar acciones disponibles.
- Ejecutar acciones mínimas con safety guard.

Realizado:

- Solución .NET y proyectos core.
- Observation, Actions, Safety, Verification y WindowsComputerUse.
- Lectura de contexto/UIA y modelos de acciones.
- Guards fail-closed para acciones UIA.
- Bases de OCR, Win32 context y fusión de locators.

Estado:

`IMPLEMENTED_FOUNDATION_NOT_END_TO_END_PRODUCT`

Pendiente:

- Integrar WCU en un flujo de producto con consent, approval, evidence y rollback/handoff.
- QA real sobre matriz de Windows soportada.
- Packaging y permisos operativos.

No reabrir:

- Micro-refactors de contratos UIA sin impacto en el vertical slice.

### Etapa B — Browser Runtime, CDP y ChromeLab

Objetivo original:

- Browser skills controladas.
- Runtime local y CDP.
- Bridge de laboratorio.
- Extensión como cliente/compatibilidad.

Realizado:

- BrowserRuntime, BrowserPerception, BrowserExecutor.Contracts y BrowserExecutor.Cdp.
- ChromeLab bridge con HTTP, WebSockets, sessions, run manager, clients y eventos.
- Extensión sidepanel con conexión, health, debug, run lifecycle, handoff, recipes y learning.
- Reconexión, heartbeat, límites de mensajes y token local.
- Session API read-only para create/capture/snapshot/latest/close.
- CBPR 001–010 fixture-safe cerrado.

Hallazgo de alineación:

- ChromeLab/extension creció hasta parecer runtime de producto.
- Esto contradice la decisión permanente de CloakBrowser como runtime canónico si no se mantiene explícitamente como lab/legacy/transition.

Estado:

`LAB_RUNTIME_SUBSTANTIAL_PRODUCT_RUNTIME_NOT_CANONICAL`

Pendiente P0:

- Auth uniforme HTTP.
- Estado autenticado obligatorio en WebSocket extension.
- Auth y enablement gate en WebSocket stealth.
- CORS por URI exacta e IDs permitidos.
- Revisar pairing local y exposición de token.
- Deshabilitar LAN hasta cerrar auth.

Pendiente P1:

- Definir adapter canónico CloakBrowser/CDP directo.
- Migrar capacidades necesarias desde extensión sin copiar su monolito.
- Cerrar la extensión como legacy/lab después de equivalencia funcional.

### Etapa C — Agent Operations, recipes y orchestration

Objetivo original:

- Contratos de Mission/Task/Recipe/Worker.
- Orquestación in-process.
- Registry de packages/skills.
- Scheduled read-only runs.
- Adapter browser gobernado.

Realizado:

- AgentOperations.Contracts, Core y Browser Adapter.
- Contratos, manifests, registries y facade in-process.
- Recipe/step libraries, risk classification y completion gates.
- Scheduled read-only contracts.
- Eventos y evidence schemas.

Estado:

`FOUNDATION_IMPLEMENTED_PRODUCT_LOOP_PARTIAL`

Pendiente:

- Reducir contratos que no participen del vertical slice.
- Elegir un único Mission/Task execution path canónico.
- Integrar Assignment → Approval → Executor → Verification → Evidence.
- Eliminar duplicaciones entre Core, AgentOperations y capas históricas.

### Etapa D — Approval, Evidence, Timeline y ejecución controlada

Objetivo original:

- ExecutionRequest y PolicyGate.
- Approval humano.
- Snapshots, verification y rollback.
- Evidence/Timeline auditable.

Realizado:

- Contratos de ejecución y policy gate.
- Approval Center y previews.
- Evidence items, eventos, timeline y redaction foundations.
- Controlled file operation y guards de rollback en líneas previas.
- Durable audit trail local/test-safe.
- Privacy export controlled execution y auditorías relacionadas.
- Diagnostics y operator surfaces read-only.

Estado:

`STRONG_CONTROL_PLANE_INCOMPLETE_PRODUCT_INTEGRATION`

Pendiente:

- Unificar el flujo en una única experiencia productiva local.
- Evitar que cada frontera cree otro packet/presenter/evidence wrapper.
- Definir evidencia mínima obligatoria por acción, no evidencia duplicada por capa.
- Completar una operación reversible útil de punta a punta.

### Etapa E — Mission Control, Research OS y sidepanel

Objetivo original:

- Mission Control visible.
- Timeline vertical.
- Workspace, approvals, agents, models y evidence.
- UX local-first coherente.

Realizado:

- Shells y previews Research OS.
- Sidepanel migrado visualmente.
- Mission hero, status panels, timeline/evidence y advisor concepts.
- Visual QA estática, screenshots y microfixes.
- Workspace Understanding V2 local/read-only.
- Mission context, plan, proposals y candidates en modo read-only/no-op.

Estado:

`VISUAL_FOUNDATION_ADVANCED_PRODUCT_WORKFLOW_FRAGMENTED`

Pendiente:

- Consolidar una sola UI canónica.
- Eliminar paneles internos/diagnósticos visibles al usuario común.
- Conectar UI a estados reales del vertical slice.
- Onboarding de primera misión.
- Estados vacíos, errores, recovery y human handoff.
- QA interactiva repetible y no basada sólo en screenshots históricas.

### Etapa F — Workspace, consent y Project Understanding

Objetivo original:

- Workspace local autorizado.
- Path jail.
- Consent por capability.
- Project understanding seguro.

Realizado:

- Workspace models y storage metadata.
- Path jail y políticas de exclusión.
- Consent scopes, per-capability gates y fail-closed evidence.
- Dry-run, fixture matrix y simuladores sintéticos.
- Secret detection policies y no-mutation proofs.

Estado:

`GOVERNANCE_ADVANCED_REAL_SCAN_PRODUCTIZATION_INCOMPLETE`

Pendiente:

- Real bounded scan con opt-in, budgets y cancellation.
- Índice local útil y incremental.
- Redaction real antes de LLM/provider.
- Presentar resultados en Mission Control.
- Medir latencia, memoria y repos grandes.

### Etapa G — BYOK, modelos, budget y Assignment Engine

Objetivo original:

- Provider registry.
- BYOK/local/managed selection.
- Budget controls.
- TaskGraph y plan editable.

Realizado:

- Diseño/config previews de providers y budget.
- OpenAI client específico en ChromeLab.
- TaskGraph, mission plan draft y assignment previews.
- Governance y handoff de planner.

Estado:

`DESIGN_AND_PREVIEW_PARTIAL_RUNTIME`

Pendiente:

- Provider abstraction canónica independiente de ChromeLab.
- Secure secret store del sistema operativo.
- Test connection sin filtrar secretos.
- Budget enforcement real.
- TaskGraph persistente y editable.
- Ejecución de una tarea aprobada con evidence.
- Fallback real y visible, sin prometer multi-provider antes de implementarlo.

### Etapa H — Expert Advisor

Objetivo original:

- Asesor no ejecutor.
- Riesgos, contradicciones y recomendaciones.

Realizado:

- Diseño, perfiles, intervention levels y visual cards.
- Guardrail conceptual de no-executor.

Estado:

`DESIGN_ADVANCED_RUNTIME_MINIMAL`

Pendiente:

- Integrarlo sólo después del vertical slice.
- Usar evidence real como input.
- Persistir sugerencias y decisiones del operador.
- No permitir que bloquee el MVP base.

### Etapa I — Product Ledger y auditoría durable

Objetivo original:

- Ledger local de decisiones, approvals, evidence y resultados.
- Superficie operativa local/dev.

Realizado:

- Product Ledger local-only/evidence-only.
- Modelos, routes/previews locales, visual QA y operator handoff.
- Durable local append-only evidence y checkpoint comparisons.
- Metadata estructurada de blockers y operator signals.

Estado:

`LOCAL_EVIDENCE_CAPABLE_NO_PRODUCT_AUTHORITY`

Pendiente:

- Consolidar modelos duplicados.
- Determinar qué parte es feature visible y cuál infraestructura.
- Integrar el ledger al vertical slice sin convertirlo en producto separado.
- No avanzar KMS/WORM/cloud antes de necesidad comercial real.

### Etapa J — Controlled Execution Design y runtime local/dev

Objetivo original:

- Diseñar ejecución segura antes de habilitarla.
- Preview, approval, verification y evidence.

Realizado:

- Fases de diseño/read-only cerradas.
- Active Safe Mode y fail-closed entrypoints.
- Operator previews y metadata de blocked frontier.
- Runtime local/dev readiness visible.

Estado:

`DESIGN_TRACK_CLOSED_LOCAL_PRODUCT_SLICE_PARTIAL`

Pendiente:

- Una ejecución útil, limitada, reversible y aprobada.
- No seguir agregando readiness packets.
- Medir resultado real en una misión local.

### Etapa K — ChromeLab local/dev operator surface

Objetivo original:

- Superficie visible/testeable sin live browser authority.

Realizado:

- Presenter, acceptance canónica, route loopback y renderer HTML.
- Read-only/fail-closed, 27% de readiness de esa línea.
- Acción deshabilitada con blocked frontier/operator signal.
- Eliminación posterior de wrappers redundantes: 530 líneas menos.

Estado:

`LAB_SURFACE_COMPLETE_ENOUGH_CLOSE_LINE_AFTER_SECURITY_HARDENING`

Pendiente:

- Auth/origin hardening del bridge.
- Validación build/test real.
- Cierre formal de la línea.
- Retorno al runtime canónico CloakBrowser y al vertical slice MVP.

## 5. Qué está realmente terminado

Terminado y preservable:

- Arquitectura local-first y fail-closed.
- Contratos base de execution, policy, evidence y verification.
- Path jail y políticas de no-mutation.
- CBPR fixture-safe cerrado.
- Read-only browser session contracts.
- Guards WCU/UIA y redaction foundations.
- Mission Control visual direction y sidepanel foundation.
- Product Ledger local evidence foundation.
- Operator surfaces locales y disabled action previews.
- Reconciliación inicial de bloat ChromeLab.

Terminado sólo como diseño/fixture/readiness, no como producto:

- Gran parte de Project Understanding.
- Provider/BYOK general.
- Assignment planner completo.
- Advisor AI.
- Browser live/product authority.
- WCU productivo.
- Export/download de usuario.
- CI/release/commercial.

## 6. Pendientes que no deben confundirse con bugs menores

P0 — Bloquean cualquier productización:

1. Bridge auth HTTP/WebSocket/stealth.
2. CORS exacto y extension allowlist.
3. Pairing/token exposure review.
4. Canonical branch y CI mínimo.
5. Vertical slice único definido y testeado.

P1 — Bloquean MVP vendible:

1. CloakBrowser/CDP direct adapter integrado.
2. Workspace onboarding real.
3. Mission → Plan → Approval → Action → Verification → Evidence.
4. Secure BYOK storage y provider abstraction mínima.
5. UI canónica conectada a estados reales.
6. Installer y updater.
7. Export/handoff usable.

P2 — Necesarios para beta estable:

1. Project Understanding real bounded.
2. Crash recovery y persistence migrations.
3. Telemetry local/opt-in y diagnostics sanitizados.
4. Test tiers y tiempos de suite.
5. Docs de operación y soporte.
6. Matriz Windows/CloakBrowser soportada.

P3 — Post-MVP:

1. Multi-provider amplio.
2. Advisor avanzado.
3. WCU amplio.
4. LAN/team/cloud sync.
5. KMS/WORM.
6. Managed AI, billing y enterprise controls.

## 7. Desvíos y sobreclaims detectados

### Sobreclaim 1 — Global roadmap readiness 98%

Corrección:

- Puede mantenerse como `Roadmap/Governance Coverage 92%`.
- Debe eliminarse como proxy de producto.
- MVP vendible real: `46%`.

### Sobreclaim 2 — Product Ledger 86%

Corrección:

- `86%` sólo puede referirse a la línea local/dev surface definida.
- Como feature integrada al producto vendible: `67%`.
- Production authority: `0%`.

### Sobreclaim 3 — Runtime/product local-dev 44%

Corrección:

- Es un índice de readiness de slices locales.
- Runtime local integrado real: `61%`.
- Production runtime: `0%`.

### Drift 1 — ChromeLab versus CloakBrowser

Corrección:

- ChromeLab queda marcado `LAB/LEGACY/TRANSITION`.
- CloakBrowser/CDP directo vuelve a ser la frontera productiva browser.
- No ampliar extension product scope.

### Drift 2 — Evidence sobre producto

Corrección:

- No crear más wrappers de evidence salvo frontera real.
- Cada nuevo bloque debe cambiar comportamiento visible, seguridad material o integración del vertical slice.

### Drift 3 — Micro-hitos y artifacts

Corrección:

- Agrupar trabajo en macro-bloques con DoD funcional.
- Dejar de versionar evidence transitoria no consumida.
- Mantener sólo fixtures, golden files y closeouts canónicos.

## 8. Roadmap canónico hacia adelante

### Macro 1 — P0 ChromeLab Bridge Security Closeout

Objetivo:

- Cerrar auth/origin/pairing y dejar el lab seguro.

DoD:

- Endpoints sensibles requieren auth.
- WebSocket extension no procesa nada antes del hello autenticado.
- Stealth exige auth y enablement explícito.
- LAN permanece disabled por defecto.
- CORS usa URI exacta y allowlist.
- Tests focales y adversariales pasan.

### Macro 2 — Canonical Branch + CI Tier 1

Objetivo:

- Tener una base reproducible.

DoD:

- Rama canónica acordada y default.
- Build core/bridge/pilot.
- Tier 1 safety.
- Secret scan.
- Sin release todavía.

### Macro 3 — MVP Vertical Slice Contract

Objetivo:

- Congelar el único flujo que se vende.

Flujo:

`Workspace local → Mission → Plan → Approval → One controlled action → Verification → Evidence/Timeline → Handoff`

DoD:

- Un solo modelo canónico por etapa.
- No duplicate packets.
- Error/retry/cancel/handoff definidos.

### Macro 4 — CloakBrowser Direct CDP Product Adapter

Objetivo:

- Cumplir la decisión de runtime único.

DoD:

- CloakBrowser pinneado.
- Adapter CDP directo.
- Capture/snapshot read-only primero.
- Acción productiva sólo dentro del vertical slice y con approval.
- Extensión no es source of truth.

### Macro 5 — Workspace + Project Understanding Bounded Real

Objetivo:

- Dar contexto real sin scan indiscriminado.

DoD:

- Consent explícito.
- Path jail.
- Exclusions/secrets.
- Budget y cancellation.
- Resultados visibles en Mission Control.

### Macro 6 — BYOK Minimum Product

Objetivo:

- Un proveedor real bien hecho antes de multi-provider.

DoD:

- Secure store.
- Test connection.
- Model selection.
- Budget visible.
- Redaction.
- Provider errors y fallback manual.

### Macro 7 — Mission Control Integrated UX

Objetivo:

- Reemplazar previews dispersas por producto coherente.

DoD:

- Onboarding.
- Mission screen.
- Approval card.
- Timeline/evidence.
- Human handoff.
- No internal diagnostic jargon en UI normal.

### Macro 8 — Controlled Action + Verification

Objetivo:

- Completar valor tangible.

DoD:

- Una acción acotada y reversible.
- Preview/diff o plan.
- Approval.
- Verification.
- Evidence.
- Failure/rollback.

### Macro 9 — Export + Installer + Updater

Objetivo:

- Convertir la plataforma en beta distribuible.

DoD:

- Installer firmado o estrategia documentada.
- Update channel.
- Versioning.
- Handoff export usable.
- Uninstall/data retention policy.

### Macro 10 — Beta Readiness

Objetivo:

- Beta privada, no release comercial plena.

DoD:

- E2E smoke.
- Windows/CloakBrowser support matrix.
- Privacy/security docs.
- Crash recovery.
- Support bundle sanitizado.
- 5–10 operadores internos/early users.

## 9. Criterio para aceptar trabajo desde ahora

Un bloque sólo avanza roadmap si cumple al menos uno:

- Reduce un riesgo P0/P1 demostrado.
- Integra una etapa del vertical slice.
- Mejora comportamiento visible al operador.
- Elimina duplicación material.
- Agrega validación necesaria para release/beta.

Rechazar:

- Copy-only.
- Guard-only sin riesgo real.
- Otro wrapper de evidence.
- Otro status packet equivalente.
- Otro artifact de micro-hito no consumido.
- Refactor amplio sin vertical slice.
- Ampliar Chrome extension como runtime productivo.

## 10. Próximo macro exacto

`AUTHORIZE_NODAL_OS_CHROMELAB_BRIDGE_AUTH_ORIGIN_PAIRING_HARDENING_AND_LAB_CLOSEOUT`

Después:

`AUTHORIZE_NODAL_OS_CANONICAL_BRANCH_CI_TIER1_AND_MVP_VERTICAL_SLICE_FREEZE`

Luego:

`AUTHORIZE_NODAL_OS_CLOAKBROWSER_DIRECT_CDP_MVP_VERTICAL_SLICE_IMPLEMENTATION`
