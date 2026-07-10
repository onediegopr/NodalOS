# NODAL OS — Auditoría Técnica Total

Fecha: 2026-07-10

## Resumen ejecutivo

Estado: `NO_GO_FOR_PRODUCTIVE_OR_LAN_RUNTIME_UNTIL_BRIDGE_AUTH_HARDENING`

Hallazgos principales:

- El bridge ChromeLab expone endpoints HTTP de control y diagnóstico sin autenticación central.
- El WebSocket de extensión valida token sólo dentro de `extension.hello`, pero no bloquea mensajes operativos enviados antes de autenticar.
- El WebSocket stealth acepta runners y mensajes sin token.
- La validación CORS usa prefijos de texto y acepta cualquier origen `chrome-extension://`; no es una frontera segura.
- La rama activa está 952 commits por delante de la rama default histórica. No hay release publicada ni CI visible.
- El repositorio contiene cientos de artefactos históricos versionados aunque `artifacts/` está ignorado.
- La solución tiene 18 proyectos de producto, dos proyectos de tests y dos tools. Safety Tests referencia 13 proyectos y funciona como suite monolítica.
- La línea ChromeLab agregó capas sucesivas de acceptance evidence con contratos y tests duplicados sin aumentar autoridad de producto.

Acción ejecutada en esta auditoría:

- Eliminadas tres capas redundantes: route acceptance wrapper, HTML acceptance wrapper y serialization evidence wrapper.
- Eliminados sus dos archivos de tests wrapper.
- Reducción neta de código/tests: 530 líneas y cinco archivos.
- Se mantienen el presenter, la aceptación canónica de la surface, la route loopback, el renderer HTML y sus tests de comportamiento directo.

## Hallazgos por severidad

### Crítico — Control HTTP y WebSocket sin autenticación uniforme

Evidencia:

- `src/OneBrain.ChromeLab.Bridge/Program.cs`
  - `/api/runs`, `/api/runs/{runId}/stop`, `/pause` y `/resume` no muestran validación de token.
  - `/debug`, `/clients`, `/last-events`, `/runtime` y `/metrics` exponen estado operativo sin gate central.
  - `/ws/stealth` acepta la conexión y procesa `stealth.hello`, friction signals, handoff y results sin token.
- `src/OneBrain.ChromeLab.Bridge/Sessions/ExtensionMessageHandler.cs`
  - `extension.hello` valida token.
  - `extension.ping` y `tool.result` no verifican que el cliente haya completado un hello válido.

Impacto:

- Riesgo: crítico si `AllowLan=true`; alto incluso en loopback frente a procesos locales o navegadores comprometidos.
- Esfuerzo: medio.
- Ganancia: muy alta.

Acción:

1. Agregar autenticación central para endpoints de control y diagnóstico.
2. Mantener `/health` mínimo y la route read-only local como únicas excepciones explícitas.
3. Exigir primer mensaje autenticado en `/ws/extension`; cerrar con código 1008 ante token o protocolo inválido.
4. Exigir token para `/ws/stealth` y no mapear/aceptar runners cuando `StealthEnabled=false`.
5. Agregar tests de HTTP 401/404, WebSocket pre-auth, token inválido, LAN y replay básico.

### Alto — CORS basado en prefijos

Evidencia:

- `src/OneBrain.ChromeLab.Bridge/Program.cs`
  - `origin.StartsWith("chrome-extension://")` acepta cualquier extensión.
  - `origin.StartsWith("http://localhost")` y `origin.StartsWith("http://127.0.0.1")` aceptan hosts con prefijo engañoso.

Impacto:

- Riesgo: alto.
- Esfuerzo: bajo.
- Ganancia: alta.

Acción:

- Parsear `Origin` con `Uri.TryCreate`.
- Comparar scheme, host y puerto exactos.
- Configurar IDs de extensión permitidos; no permitir cualquier extensión.
- Tratar CORS sólo como control de navegador, nunca como autenticación.

### Alto — Rama default obsoleta y sin gate de integración

Evidencia:

- Rama default del repositorio: `wip/hito-004b-target-window-selection`.
- Rama activa `chrome-lab-001-extension-local-ai-bridge`: 952 commits ahead del default histórico.
- GitHub muestra cero releases publicadas.
- No se encontró workflow de build/test activo.

Impacto:

- Riesgo: alto para release, onboarding, revisión y recuperación.
- Esfuerzo: medio.
- Ganancia: muy alta.

Acción:

- Crear una rama canónica estable y convertirla en default.
- Protegerla con PR obligatorio, build focal, Safety Tier 1 y secret scan.
- Mantener la rama ChromeLab como integración hasta reconciliar historial.
- No declarar release readiness antes de ese cierre.

### Alto — Explosión de artefactos versionados

Evidencia:

- El compare histórico alcanza cientos de archivos bajo `artifacts/agent-operations/*`.
- `.gitignore` ya contiene `artifacts/`, por lo que la mayor parte es legado tracked.
- Existen múltiples packs JSON/HTML por micro-hito, screenshots y reportes repetitivos.

Impacto:

- Riesgo: medio para runtime, alto para mantenibilidad y revisión.
- Esfuerzo: medio/alto.
- Ganancia: alta.

Acción:

- Inventariar artefactos canónicos frente a evidencias transitorias.
- Mantener sólo fixtures/golden files usados por tests y closeouts finales.
- Mover evidencias históricas a release artifacts o archivo externo.
- Ejecutar limpieza por lotes con verificación de referencias.

### Medio — Capa de control convertida en producto

Evidencia:

- La línea ChromeLab acumuló presenter, acceptance packet, route wrapper, route acceptance wrapper, HTML renderer, HTML acceptance wrapper y serialization wrapper.
- Varias capas repetían `LocalDevOnly`, `ReadOnly`, `FailClosed`, `ActionDisabled`, `UnsafeCapabilitiesUnavailable` y `SafeNextStep`.

Impacto:

- Riesgo: medio.
- Esfuerzo: bajo.
- Ganancia: media/alta.

Acción ejecutada:

- Eliminados wrappers de route acceptance, HTML acceptance y serialization evidence.
- Mantener validaciones directas en tests del route y renderer.
- No crear una nueva clase de evidence si no cambia una frontera real o una salida consumida.

### Medio — Suite Safety monolítica

Evidencia:

- `tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj` referencia 13 proyectos de producto.
- Usa paquetes preview de test SDK/MSTest.
- La suite mezcla static guards, browser, Product Ledger, ChromeLab, runtime y contratos.

Impacto:

- Riesgo: medio por tiempos, flakiness y blast radius.
- Esfuerzo: medio.
- Ganancia: alta.

Acción:

- Separar Tier 1 Safety, ChromeLab integration, static guards y Product Ledger.
- Mantener un agregador opcional para regresión completa.
- Congelar versiones de test estables antes de release.

### Medio — Monolitos operativos

Evidencia:

- `src/OneBrain.ChromeLab.Bridge/Program.cs` concentra hosting, CORS, API, WebSockets, stealth, helpers y logging.
- `browser-extension/onebrain-chrome-lab/service_worker.js` concentra conexión, estado, recetas, learning, handoff y UI messaging.

Impacto:

- Riesgo: medio.
- Esfuerzo: alto si se hace de una vez.
- Ganancia: alta si se divide incrementalmente.

Acción:

- Extraer primero auth/origin policy, endpoint mapping y stealth session handler.
- En extensión, extraer bridge client y run controller sin cambiar mensajes ni storage keys.
- No hacer reescritura amplia; migrar con tests de protocolo.

### Medio — Cloud/deploy no inventariado

Evidencia:

- `apps/nexa-test-owned-target` define un target Vercel estático, sintético y read-only.
- El propio README indica que desplegarlo no prueba cierre operativo.
- No existe un inventario canónico de environments, URLs, responsables, secretos, health checks o rollback.

Impacto:

- Riesgo: medio.
- Esfuerzo: bajo.
- Ganancia: alta para operación.

Acción:

- Crear `docs/operations/deployments.md` con environment, host, propósito, owner, data class, auth, deploy source y rollback.
- Mantener el target actual sólo como lab sintético.

### Bajo — Naming legado

Evidencia:

- Persisten storage keys y token prefix `nexa*`/`nexa_` en ChromeLab.

Acción:

- Mantener compatibilidad de lectura.
- Migrar escritura a nombres NODAL OS con versión explícita.
- Eliminar legado sólo después de una migración probada.

## Mapa resumido

- Core/control: `OneBrain.Core`, `Safety`, `Verification`, `SemanticState`.
- Agent operations: Contracts, Core y Browser Adapter.
- Browser: Runtime, Perception, Executor Contracts, CDP, ChromeLab Bridge y extensión Chrome.
- Windows: Observation, Actions, WindowsComputerUse, Pilot.
- Intelligence: DocumentIntelligence.
- Entry points: CLI, Pilot, ChromeLab Bridge, browser extension.
- Tests: Safety monolítico y Recipes.
- Cloud/lab: target Vercel estático; sin release/pipeline canónico.

## Plan incremental

### Bloque 1 — Seguridad ChromeLab

- Auth central HTTP.
- Auth state WebSocket extension.
- Auth/gate stealth.
- CORS exacto.
- Tests focales.

Cierre: ningún control endpoint o canal operativo acepta tráfico no autenticado.

### Bloque 2 — Integración y ramas

- Rama canónica.
- CI Tier 1.
- Secret scan.
- Release dry-run sin publicación.

Cierre: commit reproducible desde rama protegida.

### Bloque 3 — Reducción de artefactos

- Inventario automático.
- Lista de keep/remove.
- Limpieza por lotes.
- Verificación de tests y links docs.

Cierre: `artifacts/` contiene sólo fixtures o evidencia final consumida.

### Bloque 4 — Modularización medida

- Extraer auth/origin policy y endpoint maps.
- Dividir service worker por bridge/run/learning.
- Separar tests por tier.

Cierre: menor blast radius sin cambio de protocolo.

## Guardrails que permanecen

- Loopback por defecto.
- `AllowLan` opt-in explícito.
- Token generado criptográficamente y comparación constant-time.
- No exponer API keys ni tokens en payloads/diagnostics.
- Handoff humano para login, credenciales, 2FA y captcha.
- No public/product promotion automática.
- No release/commercial claim sin CI, rama canónica y evidencia.
- No eliminación masiva de artefactos sin inventario de referencias.

## Ajuste de roadmap MVP

Orden recomendado:

1. Seguridad y autenticación del bridge.
2. Flujo vendible local: misión → acción propuesta → aprobación → ejecución controlada → evidencia.
3. UI operativa mínima y coherente.
4. Installer/update/release reproducible.
5. Sólo después: stealth, LAN, cloud, automatización amplia y nuevas capas de evidence.

Próximo micro-target exacto:

`AUTHORIZE_NODAL_OS_CHROMELAB_BRIDGE_AUTH_ORIGIN_HARDENING`
