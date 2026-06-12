# HITO-127+128+129 — Evidence Index + Run Trace Linking + Harness Regression Pack

## Alcance

Este hito agrega una capa read-only de inspeccion para la evidencia del executor harness benigno.
No agrega acciones reales nuevas, no habilita clicks libres y no cambia el alcance de `/executor-harness/click`.

## HITO-127 — Evidence Index

Se agrego un indice local de evidencia para artifacts bajo:

```txt
artifacts/executor-harness/
```

El indice expone metadata segura:

- evidence id
- timestamp UTC
- harness id
- action kind
- safety decision
- verification result
- ruta logica del artifact
- link read-only de replay

La carpeta puede no existir o estar vacia. En ese caso la UI muestra un estado vacio claro y no inventa datos.

## HITO-128 — Run Trace Linking

Cada item de evidencia incluye un `ExecutorHarnessRunTraceLink`.

Estado actual:

- el artifact del harness todavia no embebe un `runId` real completo
- por eso el trace usa un id local sintetico y lo declara explicitamente
- no se inventan integraciones externas ni datos de runtime que no existan

El trace vincula:

- run id sintetico/local
- approval request
- approval decision
- safety decision
- evidence path
- replay link
- post-state result
- blocked reason si aplica

## HITO-129 — Harness Regression Pack

Se ampliaron tests de regresion del harness para cubrir:

- dry-run no ejecuta accion
- replay empty state
- replay latest artifact
- evidence index empty state
- evidence index list/latest
- trace link read-only
- fail-closed por action kind invalido
- fail-closed por target invalido
- fail-closed por approval ausente
- post-state invalido no verifica

Los tests usan fake executor. No hacen clicks reales.

## Endpoints

```txt
GET /executor-harness
GET /executor-harness/dry-run
GET /executor-harness/replay
GET /executor-harness/evidence
POST /executor-harness/click
```

`/executor-harness/evidence` es read-only.

`/executor-harness/replay` es read-only.

`/executor-harness/click` no se amplio: sigue acotado al harness benigno local y requiere aprobacion supervisada.

## Safety

Confirmaciones de diseno:

- no OpenAI real
- no API keys reales
- no playback libre
- no clicks reales automaticos
- no target configurable por input del usuario
- no MercadoLibre
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
POST /run con genera html demo OK
secret scan OK
artifacts/ ignorado/no trackeado
```

## Pendiente futuro

- Embeder `runId` real completo dentro del artifact del executor harness.
- Linkear approval/run/audit stores por ids reales cuando el harness tenga integracion completa.
- Mantener el indice como read-only incluso cuando haya mas tipos de evidencia.
