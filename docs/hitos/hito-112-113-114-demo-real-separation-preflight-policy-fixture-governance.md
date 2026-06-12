# HITO-112+113+114 - Demo/Real Separation + Preflight Policy + Fixture Governance

## Alcance

Este hito corrige la honestidad visual y la base de seguridad de Pilot sin agregar automatizacion nueva.

- HITO-112: separacion demo/runtime en Pilot.
- HITO-113: limpieza de `ClickPreflightEvaluator`.
- HITO-114: gobernanza inicial de fixtures y banner de modo demo.

## Que se corrigio

- Pilot ahora muestra origen explicito cuando una pantalla usa fixtures/demo.
- Las pantallas demo muestran `MODO DEMO / SIMULACION SEGURA`.
- Las pantallas runtime muestran `Origen: Datos locales generados por esta instalacion`.
- Los fixtures ya no deben verse como datos reales cuando los stores estan vacios.
- El flujo supervisado demo aclara que no ejecuta acciones reales, no hace clicks, no envia mensajes, no compra, no paga y no inicia sesion.
- `ExecutionAllowed=false` se explica en lenguaje de usuario: la decision se registra, pero ONE BRAIN no actua sobre otras apps.
- Los botones del playback demo usan lenguaje de simulacion, no de ejecucion real.

## Click preflight

`ClickPreflightEvaluator` fue separado en una politica declarativa:

- `ClickPreflightPolicy.Default`
- `ClickPreflightPolicy.ForAppProfile(profile)`

La politica global ya no contiene residuos de demos anteriores como `siete` ni terminos especificos de Mercado Libre como base generica. Las extensiones read-only especificas deben venir desde `AppProfile` y solo si el perfil mantiene defaults conservadores.

Acciones sensibles siguen bloqueadas o requieren aprobacion:

- comprar
- pagar
- enviar
- publicar
- borrar / eliminar
- login / iniciar sesion
- aceptar cookies
- aceptar terminos
- checkout
- carrito
- submit / send / delete / purchase / pay

Si hay duda, el resultado sigue siendo `requiresReview` o `blocked`.

## Fixtures inventariados

Fixtures usados actualmente por Pilot:

- `BusinessFlowPlaybackFixture`: flujo supervisado demo.
- `BusinessFlowDemoFixture`: aprobaciones/confianza demo.
- `HistoryDemoFixture`: historial y auditoria IA demo.
- `ProcessMemoryDemoFixture`: memoria de procesos demo.
- `AppProfileDemoFixture`: perfiles de apps/sitios demo.
- `RecordingDemoFixture`: timeline de observacion demo.

## Que sigue siendo demo

- `/flows` cuando no hay promoted flows runtime.
- `/flows/demo`
- `/playback/demo`
- `/approvals/demo`
- `/runs` cuando no hay run history runtime.
- `/ai/audit` cuando no hay audit runtime.
- `/memory` cuando no hay process memory runtime.
- `/app-profiles` cuando no hay profiles runtime.

Cada caso debe decir que muestra ejemplo de demostracion y no datos reales del usuario.

## Que sigue siendo runtime real

Datos bajo `artifacts/` generados por esta instalacion:

- `artifacts/run-history/`
- `artifacts/ai-audit/`
- `artifacts/promoted-flows/`
- `artifacts/supervised-playback/`
- `artifacts/process-memory/`
- `artifacts/app-profiles/`
- reportes Markdown/HTML de evidencia de producto

Estos artifacts siguen ignorados por Git.

## Que no se debe confundir

- Fixture no es evidencia real del usuario.
- Demo no implica que exista executor seguro.
- Aprobacion registrada no significa accion ejecutada.
- Playback supervisado demo no es playback libre.
- Read-only no habilita click real.
- AppProfile no habilita login, cookies, compra o pago por defecto.

## Pendiente

Futuro `OneBrain.DemoData` o modulo equivalente:

- mover fixtures compilados fuera de Core/Pilot donde sea conveniente;
- versionar fixtures demo con metadata de origen;
- configurar por entorno si Pilot debe mostrar ejemplos o solo empty states;
- separar definitivamente demo/test/runtime en stores y UI.

## Validacion esperada

- Build OK.
- Tests OK.
- `NEGATIVE_EXIT_CODE=1`.
- Demo script OK.
- Demo Markdown dry-run/run OK.
- Demo HTML dry-run/run OK.
- ML readonly/diagnostic OK.
- Pilot smoke OK.
- `artifacts/` ignorado/no trackeado.
- `git status --short` limpio.

## Safety

- No OpenAI real.
- No API keys reales.
- No playback libre.
- No clicks reales.
- No cookies accepted.
- No login.
- No carrito.
- No compra.
- No pago.
- No auto-open de HTML/artifacts.
- No Firefox flaky obligatorio.
