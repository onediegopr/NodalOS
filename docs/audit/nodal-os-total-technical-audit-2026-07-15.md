# NODAL OS — Auditoría Técnica Total

Date: 2026-07-15

Baseline audited: `main` after Teach NODAL Windows observation merge `473fcd8b55f7c5b78e82f835496334abfeb62aca`.

Decision: `GO_WITH_FINDINGS_TECHNICAL_FOUNDATION_STRONG_PRODUCTIZATION_REFOCUS_REQUIRED`.

## Resumen ejecutivo

No se encontró un P0 que obligue a detener el desarrollo técnico. Sí se encontraron defectos de severidad alta que impiden considerar el repositorio un MVP vendible:

- GitHub todavía presenta una rama WIP histórica como default, 1.007 commits detrás de `main` al momento de la inspección.
- El repositorio no contiene una aplicación desktop empaquetable, instalador, updater, release firmado ni canal de distribución.
- La UI principal sigue siendo un Pilot/demo interno, fragmentado y visualmente incompatible con la dirección Mission Control dark-first.
- Documentos y changelog mezclaban “vertical slice técnico”, “MVP vendible” y “producción” como si fueran el mismo estado.
- Todo el stack usa .NET 11 preview y el SDK no estaba fijado, por lo que una actualización automática podía romper CI sin cambio de código.
- Runtime tests se ejecutaban dos veces en pull requests relevantes y el smoke end-to-end vivía como 238 líneas inline dentro del workflow.
- El volumen de contratos, tests, documentos y evidencia histórica es alto para el valor de producto actualmente visible.

La conclusión de roadmap es directa: la línea Living Skills ya tiene suficiente fundamento. El siguiente dólar y la siguiente semana deben ir a producto coherente, workspace real, BYOK utilizable, packaging y private beta; no a otra expansión de contratos o memoria agentic.

## Inventario reproducible

El inventario se genera con:

```text
eng/audit/repository_audit.py
.github/workflows/repository-audit.yml
```

Baseline medido por GitHub Actions:

| Métrica | Resultado |
| --- | ---: |
| Archivos tracked | 5.397 |
| Líneas de texto | 635.463 |
| Líneas C# en `src/` | 191.496 |
| Líneas C# en `tests/` | 177.174 |
| Líneas Markdown | 159.170 |
| Ratio tests/source | 0,925 |
| Ratio docs/source | 0,831 |
| Proyectos .NET | 24 |
| Proyectos dentro de la solución | 24 |
| Ciclos entre ProjectReference | 0 |
| Métodos MSTest | 8.277 |
| Archivos C# | 1.527 |
| Records | 2.626 |
| Enums | 1.206 |
| Interfaces | 36 |
| Assert/Contains de texto aproximados | 4.873 |
| Grupos de archivos de texto idénticos | 25 |
| Archivos históricos bajo `artifacts/` | 1.548 |
| Archivos bajo `docs/` | 2.031 |
| Markdown files | 1.841 |
| Archivo vacío accidental | 1 |
| Binarios tracked | 7 |
| TODO/FIXME/HACK/XXX | 24 |

El conteo de rutas HTTP es sintáctico y se usa como señal de superficie, no como contrato exacto. El inventario encontró decenas de mappings y la revisión manual confirmó que Pilot expone numerosas rutas demo, harness e internas.

## Mapa del sistema actual

### Runtime y servicios

- `OneBrain.Core`: contratos, approval, recipes, evidence, skills, context y policy foundations.
- `OneBrain.AgentOperations.Core`: mission runtime, workspace understanding y operaciones de archivo test-owned.
- `OneBrain.Pilot`: host HTTP y superficies locales/dev.
- `OneBrain.Actions`, `OneBrain.Observation`, `OneBrain.SemanticState`, `OneBrain.Verification`: Windows UIA, acciones y verificación.
- `OneBrain.BrowserRuntime`: CloakBrowser/direct CDP target y health/preflight.
- `OneBrain.ChromeLab.Bridge`: lab/transition bridge.
- `OneBrain.BrowserExecutor.*`, `BrowserPerception`, `WindowsComputerUse`: browser/OCR/WCU foundations protegidas o parciales.

### Cliente y superficie

- No existe frontend React/Svelte/Tauri actual.
- La superficie visible primaria es HTML server-rendered por `OneBrain.Pilot`.
- Existen múltiples vistas útiles, pero se presentan como demos, fixtures, harnesses y rutas internas, no como una experiencia de cliente única.

### Infraestructura y despliegues

- GitHub Actions es la única infraestructura de validación encontrada para este producto.
- En las cuentas conectadas se inspeccionaron tres proyectos Vercel y ocho proyectos Neon; ninguno coincidía con NODAL OS/NODRIX.
- No se identificó release GitHub publicado, instalación desktop, backend NODAL, base NODAL ni despliegue productivo.
- No se mutó ningún recurso cloud durante la auditoría.

## Hallazgos por severidad

## Crítico

Ninguno.

No se encontró ejecución productiva abierta, secreto expuesto por CI, bypass directo de approval/verification ni despliegue NODAL activo desconocido.

## Alto

### H1 — Rama default histórica y 1.007 commits detrás

**Evidencia**

- Default remoto observado: `wip/hito-004b-target-window-selection`.
- `main`: 1.007 commits ahead, 0 behind.
- Desarrollo activo y PRs recientes se integran en `main`.

**Impacto**

Clones, navegación pública, integraciones y nuevos colaboradores aterrizan en una versión obsoleta. También invalida la narrativa de gobernanza existente.

**Acción**

- Cambiar default branch a `main`.
- Aplicar branch rules/protection.
- Fast-forward de ramas de compatibilidad donde sea seguro.

**Riesgo / esfuerzo / ganancia**

- Riesgo: bajo.
- Esfuerzo: bajo, requiere setting remoto.
- Ganancia: muy alta.

**Estado**

Documentado y preparado. El conector disponible no expone mutación de default branch/protection; queda una acción administrativa externa.

### H2 — No existe producto desktop instalable

**Evidencia**

- No hay root `package.json`.
- No hay `Cargo.toml` ni `src-tauri/Cargo.toml`.
- No hay proyecto de packaging, installer, updater o firma.
- No hay release publicada.

**Impacto**

El producto técnico no puede entregarse como la app desktop local-first definida por negocio. `npm`, `cargo` y `tauri build` no son validaciones pendientes: hoy no aplican porque esa capa no existe.

**Acción**

Elegir una ruta de packaging compatible con el runtime .NET real y producir un instalador Windows reproducible antes de private beta.

**Riesgo / esfuerzo / ganancia**

- Riesgo: medio.
- Esfuerzo: alto.
- Ganancia: crítica para negocio.

### H3 — Superficie de producto fragmentada e interna

**Evidencia**

- Root Pilot identificado como `ONE BRAIN Pilot`.
- Navegación orientada a demo, harness, fixtures, labs e internal routes.
- Paleta clara/marrón y estructura distinta de Mission Control dark-first.
- Doc visual requiere top bar, sidebar mínima, timeline vertical central, panel derecho y logs/evidence secundarios.

**Impacto**

La base técnica no se percibe como el producto que se quiere vender. La superficie amplifica scaffolding y conceptos internos.

**Acción**

Crear un shell único NODAL/NODRIX Mission Control; relegar rutas existentes a adapters de diagnóstico y desarrollo.

**Riesgo / esfuerzo / ganancia**

- Riesgo: medio.
- Esfuerzo: medio-alto.
- Ganancia: muy alta.

### H4 — Claims de madurez contradictorios

**Evidencia**

- `CHANGELOG.md` declaraba `v1.0.0-production` sin release publicada.
- El vertical slice fixture-safe era llamado “first sellable MVP”.
- Roadmaps históricos atribuyen porcentajes de producción y stacks que ya no representan el repositorio actual.

**Impacto**

Riesgo de decisiones, marketing, handoffs y desarrollo basados en un estado inexistente.

**Acción aplicada**

- Changelog corregido.
- README canónico agregado.
- Vertical slice renombrado y acotado como prueba técnica de seguridad.
- Roadmap compacto con readiness recalibrado.

**Riesgo / esfuerzo / ganancia**

- Riesgo: bajo.
- Esfuerzo: bajo.
- Ganancia: alta.

### H5 — Toolchain preview móvil y no reproducible

**Evidencia**

- 13 proyectos `net11.0` y 11 `net11.0-windows`.
- Workflows instalaban `11.0.x` + `preview`.
- SDK validado en CI: `11.0.100-preview.6.26359.118`.
- Test SDK y MSTest también son previews exactos.

**Impacto**

Una nueva preview podía cambiar compilador/runtime sin commit, generando fallos difíciles de reproducir.

**Acción aplicada**

- `global.json` exacto.
- Workflows fijados a la misma preview validada.

**Riesgo / esfuerzo / ganancia**

- Riesgo: bajo.
- Esfuerzo: bajo.
- Ganancia: alta para reproducibilidad.

### H6 — Monolitos de código y contratos

Archivos principales:

| Líneas | Archivo |
| ---: | --- |
| 6.399 | `src/OneBrain.Cli/Recipes/RecipeRunner.cs` |
| 2.490 | `src/OneBrain.Pilot/PilotHomePageRenderer.cs` |
| 2.255 | `src/OneBrain.Core/Recipes/RecipeProductSurfaceContracts.cs` |
| 2.166 | `src/OneBrain.BrowserExecutor.Cdp/NodalOsOcrDictionaryCompatibilityServices.cs` |
| 2.006 | `src/OneBrain.BrowserExecutor.Cdp/NodalOsOcrVisionServices.cs` |
| 1.857 | `src/OneBrain.Pilot/ProductLedgerLocalDevRouteEndpointMapper.cs` |
| 1.728 | `src/OneBrain.Core/Context/WorkspaceContextReadOnlyFoundation.cs` |
| 1.577 | `src/OneBrain.Core/Evidence/EvidenceIntelligencePersistenceDesign.cs` |

**Impacto**

Mayor acoplamiento, revisión lenta, conflicto de cambios, discoverability baja y tentación de sumar contratos en vez de producto.

**Acción**

No hacer una refactorización masiva. Extraer por seams de comportamiento al tocar cada macro de producto:

1. RecipeRunner por command family.
2. Pilot renderer por product section.
3. Product Ledger local/dev mapper por route group.
4. OCR compatibility y vision services por provider/boundary.

Cada extracción debe reducir LOC/coupling y mantener tests de comportamiento, no agregar wrappers vacíos.

**Riesgo / esfuerzo / ganancia**

- Riesgo: medio-alto si se hace en masa.
- Esfuerzo: alto.
- Ganancia: alta si es incremental.

### H7 — Release legal/operativa incompleta

**Evidencia**

- Repositorio público sin `LICENSE`.
- No existía `SECURITY.md`.
- No hay privacy/release package consolidado.

**Impacto**

No debe distribuirse comercialmente sin una decisión explícita de licencia y obligaciones de terceros.

**Acción aplicada / pendiente**

- `SECURITY.md` agregado.
- La selección de licencia queda bloqueador de release porque es una decisión legal/comercial, no una inferencia técnica.

## Medio

### M1 — CI duplicaba la suite Runtime

Tier 1 y Selective Runtime ejecutaban `OneBrain.Runtime.Tests` en el mismo PR relevante.

**Acción aplicada**

- Runtime suite queda propiedad de `runtime-integration`.
- Tier 1 conserva ChromeLab/Safety y secret scan.
- Trigger runtime simplificado a `src/**` más tests focales.

### M2 — Smoke CI de 238 líneas inline

**Acción aplicada**

Se extrajo a `eng/ci/smoke-pilot-product-loops.ps1`, reutilizable y revisable fuera del YAML.

### M3 — Tests copy-heavy y costosos de mantener

- 8.277 métodos MSTest.
- 177.174 líneas de test frente a 191.496 líneas de source.
- Miles de asserts sobre strings/contains.
- Archivo de test máximo: 2.859 líneas.

**Acción**

Mantener guardrails, pero consolidar pruebas de copy/metadata en tablas y snapshots estables. Priorizar comportamiento: estado, side effects, redaction, authority y verification.

No iniciar una poda masiva sin mapa de cobertura.

### M4 — Documentación y micro-hitos dominan el repositorio

- 159.170 líneas Markdown.
- 1.841 Markdown files.
- `docs/decision-log.md`: 3.622 líneas.
- `docs/roadmap/nodal-os-roadmap-vnext.md`: 3.806 líneas.
- 348 tokens `AUTHORIZE_NODAL_OS_` y 316 ocurrencias de `Resulting state`.

**Impacto**

La documentación operativa se volvió una segunda carga de producto y dificulta localizar la fuente actual.

**Acción**

- El roadmap compacto pasa a ser la puerta de entrada.
- Archivar por fecha las narrativas históricas; no seguir concatenando cada micro-target al mismo archivo.
- Mantener decision log de decisiones materiales, no de cada rename o selector.

### M5 — Evidencia histórica tracked

- 1.548 archivos bajo `artifacts/`.
- 53.652 líneas de texto y binarios de screenshots.
- 25 grupos exactos de texto duplicado, concentrados en evidencia histórica/manual QA.

**Acción**

Usar el cleanup reference-aware existente en un PR de mantenimiento separado. No se borró evidencia durante esta auditoría porque referencias y golden/fixtures deben preservarse.

El workflow de cleanup fue apuntado al branch canónico `main`.

### M6 — Safety aggregate muy acoplado

`OneBrain.Safety.Tests` referencia 13 proyectos y excluye manualmente archivos ChromeLab del compile.

**Acción**

Mantener por ahora. En el próximo ciclo de test-infra, convertir Safety aggregate en tests de arquitectura/guardrails y mover tests funcionales pesados a proyectos propietarios, evitando otro mega-suite.

### M7 — Naming comercial no congelado

Código y repo usan NODAL OS/ONE BRAIN; documentos de negocio usan NODRIX; un documento visual todavía usa HOTEP.

**Acción**

Tomar una decisión de naming antes del shell final y migrar sólo copy/product identity. No renombrar namespaces, paquetes o contratos durante el MVP.

## Bajo

- Un warning MSTEST0037 preexistente permanece en Recipes tests.
- 24 TODO/FIXME/HACK/XXX no representan deuda crítica, pero deben revisarse al tocar cada archivo.
- Siete imágenes/binarios tracked son evidencia/QA y no se eliminan sin migración.
- El inventario de secret-shapes marca 52 paths para revisión manual; el secret scan de PR continúa verde y no se imprimen valores.

## Correcciones ejecutadas en esta ventana

| Corrección | Estado |
| --- | --- |
| Cierre PR Windows observation, 143/143 Runtime + CI/Tier 1 | Hecho y fusionado |
| Inventario determinista JSON/Markdown | Hecho |
| Archivo vacío `nonexistent` | Eliminado |
| Changelog con claim falso de producción | Corregido |
| README canónico | Agregado |
| Security policy | Agregada |
| SDK .NET preview | Fijado en `global.json` y workflows |
| Runtime tests duplicados en CI | Eliminada la duplicación |
| Smoke PowerShell inline | Extraído a script reusable |
| Trigger Selective Runtime incompleto | Simplificado a `src/**` y suites focales |
| Workflow de artifact cleanup sobre branch legacy | Apuntado a `main` |
| Vertical slice confundido con producto vendible | Reencuadrado como safety slice técnico |
| Roadmap actual | Compactado y orientado a producto |
| Branch governance | Actualizada con estado remoto real |

## Plan incremental de migración

### Paso 1 — Cierre remoto de gobernanza

- default branch `main`;
- reglas de PR, no-force-push, no-delete;
- checks reales y sin contexts retirados;
- fast-forward de ramas legacy compatibles.

Prueba: clone limpio abre `main`; PR de docs no queda pendiente por un check path-scoped; PR de source ejecuta Runtime Integration.

### Paso 2 — Shell Mission Control único

- nuevo entrypoint dark-first;
- timeline central;
- navegación mínima;
- UI interna/demo detrás de dev mode;
- copy NODAL/NODRIX consistente.

Pruebas: accesibilidad, responsive, no secret/path leak, navegación, no acciones internas expuestas, smoke HTML/desktop.

### Paso 3 — Workspace real y una acción reversible

- selector de workspace explícito;
- storage local de configuración no sensible;
- allowlist de operación;
- precondition hash, approval, snapshot, atomic write, verification, rollback;
- evidence/handoff redacted.

Pruebas: traversal, symlink/reparse, stale hash, no-op, overwrite, cancellation, rollback, cleanup, user-selected root, cross-workspace denial.

### Paso 4 — BYOK real

- secure store;
- provider connection test;
- one-call vertical slice;
- fallback policy;
- budgets/cancellation;
- redaction.

Pruebas: 401, 429, timeout, cancel, same-privacy fallback, local-to-cloud block, no secret in logs/evidence.

### Paso 5 — Packaging/private beta

- Windows installer;
- versioning;
- install/uninstall/upgrade;
- crash/recovery;
- security/license/privacy pack;
- beta onboarding.

Pruebas: clean VM install, first run, workspace select, BYOK, one mission, one action, handoff, update/rollback.

## Guardrails que deben permanecer intactos

- [x] aprobación por misión/scope para trabajo ordinario;
- [x] intervención ante scope material, secrets, external communication, destructive work, privacy/cost escalation;
- [x] observed content is data, not authority;
- [x] semantic verification before skill/evidence promotion;
- [x] no raw secret, DOM, screenshot or absolute path in learned memory/handoff;
- [x] no system Chrome/Edge/default Chromium fallback;
- [x] ChromeLab lab-only; CloakBrowser canonical target;
- [x] no customer data or production claim from fixtures;
- [x] Advisor remains non-executor;
- [x] no second ledger, timeline, policy engine or database created by this audit.

## Ajuste final de roadmap

Pausar expansión de Living Skills y Browser Automation.

Prioridad inmediata:

```text
Mission Control coherente
→ workspace local real
→ BYOK real
→ una operación reversible
→ evidence/handoff
→ installer/private beta
```

Macro recomendado:

`NODAL_OS_PRODUCTIZATION_MISSION_CONTROL_AND_REAL_LOCAL_WORKSPACE_MVP`

## Estado de salida

`AUDIT_COMPLETE_WITH_ONE_EXTERNAL_GITHUB_SETTINGS_BLOCKER_AND_RELEASE_GAPS_IDENTIFIED`

La auditoría técnica y sus correcciones seguras pueden cerrarse. El roadmap debe retomar desde productización, no desde otra fundación agentic.
