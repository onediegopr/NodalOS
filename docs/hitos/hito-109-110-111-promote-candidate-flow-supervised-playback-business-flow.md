# HITO-109+110+111 - Promote Candidate Flow + Supervised Playback v0 + First Usable Business Flow

## Alcance

Este bloque conecta recording/timeline/anotacion con un flujo supervisado usable desde Pilot:

- Promover una timeline/candidate flow a flujo aprobado para playback supervisado.
- Ejecutar el flujo paso a paso bajo control humano.
- Registrar evidencia local de playback, historial y auditoria operacional.
- Mostrar un primer flujo de negocio demo tipo presupuesto/mensaje, sin envio real.

## HITO-109 - Promote Candidate Flow

Se agrego:

- `CandidateFlowPromotionRequest`
- `CandidateFlowPromotionResult`
- `CandidateFlowStatuses`
- `PromotedCandidateFlow`
- `PromotedFlowStep`
- `CandidateFlowPromotionService`
- `PromotedFlowStore`

La promocion solo pasa si:

- El linter fue marcado OK.
- Las variables estan resueltas o declaradas.
- La politica de riesgo es consistente.
- Existe politica de aprobacion cuando hay pasos sensibles.
- No hay acciones bloqueadas.
- No hay pasos con confianza insuficiente.

La promocion falla cerrado y no genera recipes ejecutables.

## HITO-110 - Supervised Playback v0

Se agrego:

- `SupervisedPlaybackSession`
- `SupervisedPlaybackStepState`
- `SupervisedPlaybackService`
- `SupervisedPlaybackStore`

El playback permite:

- Ver paso actual y estado de sesion.
- Confirmar paso supervisado.
- Saltar paso si la politica lo permite.
- Abortar flujo.
- Registrar evidencia local.
- Registrar run history.

Reglas vigentes:

- No playback autonomo completo.
- No acciones peligrosas sin aprobacion.
- Si no hay executor seguro, se bloquea.
- Si el paso es sensible y falta aprobacion, se bloquea.
- Si el paso es demo/fixture, se muestra como simulacion segura.

## HITO-111 - First Usable Business Flow

El primer flujo usable es una simulacion segura tipo presupuesto/mensaje:

1. Seleccion de flow.
2. Variables declaradas.
3. Validacion conceptual de promocion.
4. Aprobacion humana si corresponde.
5. Playback supervisado paso a paso.
6. Resultado con evidencia.
7. Historial local.

No envia mensajes, no usa WhatsApp real y no interactua con navegador vivo.

## Pilot UI

Rutas agregadas:

- `/flows`
- `/flows/demo`
- `/flows/demo/promote`
- `/playback/demo`
- `/playback/demo/confirm`
- `/playback/demo/skip`
- `/playback/demo/abort`

La UI deja claro:

- Playback autonomo desactivado.
- Humano al mando.
- Sin acciones peligrosas.
- Evidencia generada local.

## Safety

Confirmado por diseno:

- No OpenAI real.
- No API keys reales.
- No playback libre.
- No clicks reales.
- No login.
- No cookies accepted.
- No carrito.
- No compra.
- No pago.
- No envio real.
- No auto-open de HTML/artifacts.

## Validacion esperada

- Build OK.
- Tests OK.
- `NEGATIVE_EXIT_CODE=1`.
- Demo script OK.
- Demo Markdown dry-run/run OK.
- Demo HTML dry-run/run OK.
- ML readonly/diagnostic OK si no tarda demasiado.
- Pilot smoke OK incluyendo `/flows` y `/playback/demo`.
- `artifacts/` ignorado por Git.

## Limitaciones

- No genera recipes ejecutables finales.
- No ejecuta acciones reales de UI Automation.
- No hace playback autonomo.
- No resuelve ambiguedad con IA real.
- No conecta OpenAI real.
