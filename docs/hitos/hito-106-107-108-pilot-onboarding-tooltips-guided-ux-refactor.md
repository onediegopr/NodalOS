# HITO-106+107+108 - Pilot Onboarding + Tooltips + Guided UX Refactor

## Alcance

Refactor de UX para que ONE BRAIN Pilot sea entendible para un usuario nuevo sin cambiar la arquitectura base, sin agregar OpenAI real y sin habilitar acciones peligrosas.

- HITO-106: sistema simple de ayuda contextual reusable con tooltips, help text y hints de conceptos.
- HITO-107: home guiada con recorrido paso a paso y accesos claros para "probar ahora".
- HITO-108: lenguaje mas claro para usuario final manteniendo los terminos internos entre slash o en contexto.

## Cambios aplicados

- Home reorganizada para explicar:
  - que es ONE BRAIN Pilot
  - que puede probarse ahora
  - que no hace por seguridad
  - flujo recomendado paso a paso
  - glosario minimo de conceptos clave
- Navegacion comun con nombres mas claros:
  - Tareas automatizables / Recipes
  - Datos de la tarea / Variables
  - Historial de ejecuciones / Runs
  - Decisiones IA / AI Audit
  - Apps y sitios soportados / App Profiles
  - Procesos aprendidos / Memory
  - Aprobaciones humanas / Approvals
- Ayuda contextual reusable sin JavaScript externo:
  - `HelpTooltip`
  - `HelpText`
  - `ConceptHint`
  - `EmptyStateNotice`
- Estados vacios mas explicativos en recipes, variables, memory, app profiles, runs y AI audit.
- Safety summary siempre visible con mensaje claro de modo seguro.

## Safety

- No clicks
- No login
- No cookies accepted
- No carrito
- No compra
- No pago
- No playback
- No OpenAI real
- No auto-open de HTML ni artifacts
- No comandos arbitrarios

## Validacion esperada

- Build OK
- Tests OK
- `NEGATIVE_EXIT_CODE=1`
- Demo script OK
- Demo Markdown dry-run/run OK
- Demo HTML dry-run/run OK
- ML readonly/diagnostic OK
- Pilot routes principales HTTP 200
- Runtime artifacts ignorados por Git

## Archivos principales

- `src/OneBrain.Pilot/PilotHomePageRenderer.cs`
- `tests/OneBrain.Recipes.Tests/PilotShellTests.cs`
- `tests/OneBrain.Recipes.Tests/PilotGuiUxHardeningTests.cs`
