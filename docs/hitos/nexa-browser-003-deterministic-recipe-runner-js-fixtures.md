# NEXA Browser-003 - Deterministic Recipe Runner + JS Fixture Tests

## Objetivo

Este hito convierte `Recipe Mode` de NEXA Chrome Operator Lab de replay V0 por instruccion a un runner deterministico paso a paso.

No toca Windows/UIA.

## Implementado

### Recipe Schema V1

Se agrego `recipe_core.js` con:

* `schemaVersion: 1`
* `recipeId`
* `name`
* `description`
* `createdAt`
* `updatedAt`
* `startUrl`
* `parameters`
* `steps`
* `humanCheckpoints`
* `safety`
* `metadata`

Tipos de parametros soportados:

* `text`
* `number`
* `money`
* `date`
* `select`
* `boolean`

Tipos de pasos soportados:

* `navigate`
* `observe`
* `resolveTarget`
* `click`
* `input`
* `select`
* `wait`
* `verify`
* `humanCheckpoint`

### Learning -> Recipe V1

Learning Mode ahora convierte el draft capturado a Recipe Schema V1.

Reglas:

* clicks humanos -> step `click`
* inputs no sensibles -> step `input` con parametro `{{paramName}}`
* selects -> step `select`
* navegacion -> step `navigate`
* password/credential-like -> step `humanCheckpoint`

No se guardan valores de password ni credenciales.

### Runner deterministico

El service worker ahora mantiene estado explicito:

* `recipeRunId`
* `recipeId`
* `currentStepIndex`
* `currentStepId`
* `currentStepLabel`
* `status`
* `startedAt`
* `updatedAt`
* `completedAt`
* `lastError`
* `stepResults`

Estados:

* `idle`
* `running`
* `paused`
* `waitingHuman`
* `completed`
* `failed`
* `stopped`

Cada step result registra:

* `stepId`
* `type`
* `status`
* `startedAt`
* `completedAt`
* `summary`
* `toolRequest`
* `toolResult`
* `verificationResult`
* `error`

### Ejecucion de pasos

El runner ejecuta:

* `navigate` con validacion de URL permitida
* `observe` via `observePage`
* `resolveTarget` con threshold minimo
* `click` via `clickElement`
* `input` via `setElementValue`
* `select` via `selectOption`
* `wait` con polling
* `verify` con observacion
* `humanCheckpoint` pausando el runner

### Recuperacion basica

La pestaña Recetas permite:

* ejecutar receta
* pausar
* reanudar
* reintentar paso
* saltar paso seguro (`observe`, `wait`, `verify`)
* abortar

Si un paso falla:

* la receta queda `failed`
* se muestra razon clara
* queda disponible retry/abort/human intervention

### Side Panel

Recetas ahora muestra:

* parametros requeridos
* run actual
* estado
* paso actual
* error
* timeline de pasos

### JS fixture tests

Se agrego:

```text
browser-extension/onebrain-chrome-lab/tests/fixture_tests.js
```

Cubre:

* botones basicos
* formulario con password redactado
* prioridad de stable selectors
* targets ambiguos
* conversion Learning -> Recipe V1
* runner fixture simple

## Smoke manual

### Smoke 1 - Side panel

* tabs visibles
* STOP visible
* Runtime conecta

Estado: documentado, pendiente de ejecucion manual.

### Smoke 2 - Learning simple

* crear receta `Login test`
* grabar click/input no sensible
* detener
* guardar receta
* verificar Recipe Schema V1

Estado: documentado, pendiente de ejecucion manual.

### Smoke 3 - Recipe runner local/simple

* ejecutar receta guardada
* ver paso actual
* ver step timeline
* ver passed/failed
* ver verification

Estado: documentado, pendiente de ejecucion manual.

### Smoke 4 - Falla controlada

* receta con target inexistente
* debe fallar claro
* mostrar razon
* permitir reintentar o abortar

Estado: documentado, pendiente de ejecucion manual.

### Smoke 5 - AFIP/ARCA inicial

Receta simple:

```text
Entrar a AFIP/ARCA y tocar iniciar sesion. Pausar ante credenciales.
```

Esperado:

* no hardcode AFIP/ARCA
* target resolution generico
* checkpoint humano
* resume reobserva

Estado: documentado, pendiente de ejecucion manual.

## Limitaciones abiertas

* El runner es deterministico, pero aun vive completo en service worker.
* La reparacion alternativa de candidatos se expone por estado, pero no hay UI fina para elegir candidato alternativo.
* Los fixture tests usan Node sin DOM real para mantener cero dependencias.
* Falta runner con persistencia de progreso tras reinicio de service worker.

## Proximo hito recomendado

`NEXA Browser-004 - Candidate Repair UI + Persistent Recipe Run Recovery`
