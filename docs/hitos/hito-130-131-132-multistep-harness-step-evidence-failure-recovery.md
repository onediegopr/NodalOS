# HITO-130+131+132 — Multi-Step Harness Flow + Step-Level Evidence + Failure Recovery

## Alcance

Este hito extiende el executor harness benigno local con:

- un flow multi-step local y controlado
- evidencia por step embebida en artifacts del harness
- politica explicita de recuperacion fail-closed

No agrega acciones reales externas.
No agrega playback libre.
No ejecuta clicks reales automaticamente.
No cambia el alcance de `POST /executor-harness/click`.

## HITO-130 — Multi-Step Harness Flow

Se agrego un flow local benigno con dos pasos allowlisted dentro del mismo harness local:

- `step-1`
- `step-2`

Cada paso incluye:

- `stepId`
- `actionKind`
- `targetConstraints`
- `resolvedTarget`
- `expectedPostState`
- `safetyDecision`

El flow es local-only, sin targets configurables por input del usuario.

Rutas nuevas:

```txt
GET /executor-harness/flow
GET /executor-harness/flow/dry-run
```

Ambas son read-only.

## HITO-131 — Step-Level Evidence

La evidencia del harness ahora soporta steps embebidos cuando el artifact proviene de un flow multi-step.

Cada step puede registrar:

- interaction contract
- target resolution
- approval decision
- safety decision
- command summary
- pre-action state
- post-action state
- verification result
- blocked reason

Compatibilidad:

- artifacts anteriores sin `steps` siguen siendo validos
- replay e index siguen funcionando con evidencia vieja

## HITO-132 — Failure Recovery Policy

La politica implementada es:

```txt
fail_closed_stop_on_first_failed_step
```

Si un paso falla o queda bloqueado:

- el flow no continua automaticamente
- se marca blocked
- se registra el step fallido
- se preserva evidencia de pasos ya verificados
- queda requerida intervencion humana

## Vistas y replay

Se actualizaron:

- `/executor-harness`
- `/executor-harness/replay`
- `/executor-harness/evidence`

Cambios visibles:

- el harness enlaza al flow multi-step
- replay muestra steps si existen
- evidence index muestra `step count` y `flow status`
- el trace sigue siendo local/sintetico cuando no existe `runId` real embebido

## Safety

Se mantiene:

- no OpenAI real
- no API keys reales
- no clicks reales automaticos
- no target configurable por input del usuario
- no cookies accepted
- no login
- no carrito
- no compra
- no pago
- no auto-open de artifacts

## Validacion esperada

```txt
build OK
tests OK
NEGATIVE_EXIT_CODE=1
demo script OK
demo Markdown dry-run/run OK
demo HTML dry-run/run OK
ML readonly/diagnostic OK
Pilot HTTP smoke OK:
  /
  /executor-harness
  /executor-harness/dry-run
  /executor-harness/replay
  /executor-harness/evidence
  /executor-harness/flow
  /executor-harness/flow/dry-run
POST /run con genera html demo OK
secret scan OK
artifacts/ ignorado/no trackeado
```
