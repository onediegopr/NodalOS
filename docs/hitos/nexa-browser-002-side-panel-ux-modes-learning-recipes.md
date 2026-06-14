# NEXA Browser-002 - Side Panel UX V2 + Modes Foundation + Learning/Recipes V0

## Objetivo

Este hito convierte el side panel de NEXA en una cabina por modos:

* Operar
* Aprender
* Recetas
* Runtime

No toca Windows/UIA.

## Implementado

### Side Panel UX V2

El panel ahora tiene:

* header persistente con `NEXA`
* estado de conexion/run
* tabs principales
* STOP siempre visible
* banner global de intervencion humana

### Operar

Incluye:

* entrada principal de instruccion
* `Ejecutar`
* `Pausar`
* `Reanudar`
* objetivo actual
* plan base
* accion/tool actual
* pagina actual
* target resolution card
* verification card
* timeline operativo

### Aprender

Incluye:

* nombre de receta
* descripcion
* empezar/detener aprendizaje
* revisar receta
* guardar receta
* estado de grabacion
* timeline de acciones capturadas
* draft JSON V0

Learning Mode captura:

* navegacion
* click
* input no sensible
* select
* submit
* target metadata
* stable selectors
* contexto cercano
* hints de verificacion

No captura:

* passwords
* tokens
* valores credential-like

### Recetas

Incluye:

* lista local de recetas
* editar
* ejecutar
* duplicar
* borrar
* exportar JSON
* importar JSON
* editor JSON V0

Recipe replay V0 ejecuta la receta como instruccion al operador con soporte declarado para:

* navigate
* click
* input
* human checkpoint

No simula exito.

### Runtime

Incluye:

* motor local
* host/puerto
* health
* provider/model/API key local sin exponer key
* WebSocket
* tab activa
* URL
* content script availability
* runId
* requestId
* current tool
* last error
* logs locales
* engine -> extension
* extension -> engine
* ultimo `tool.request`
* ultimo `tool.result`
* ultimo `run.status`

## Storage

Se usa `chrome.storage.local`.

Keys:

* `nexaRecipes`
* `nexaLearningDraft`

No se guardan credenciales.

## Smoke manual

### Smoke 1 - UI general

1. Cargar extension unpacked.
2. Abrir side panel.
3. Verificar tabs:
   * Operar
   * Aprender
   * Recetas
   * Runtime
4. Verificar que STOP esta siempre visible.

Estado: documentado, pendiente de ejecucion manual.

### Smoke 2 - Runtime

1. Bridge apagado: Runtime debe mostrar desconectado.
2. Bridge encendido: Runtime debe mostrar conectado.
3. Health debe mostrar OK.
4. WebSocket debe figurar conectado.

Estado: documentado, pendiente de ejecucion manual.

### Smoke 3 - Operar

Instruccion:

```text
Toca iniciar sesion.
```

Esperado:

* `resolveTarget` visible
* candidatos visibles
* score visible
* `clickElement` visible
* verification visible

Estado: documentado, pendiente de ejecucion manual.

### Smoke 4 - Aprender

1. Empezar aprendizaje.
2. Hacer navegacion/click/input no sensible.
3. Detener aprendizaje.
4. Ver timeline capturado.
5. Guardar receta.

Estado: documentado, pendiente de ejecucion manual.

### Smoke 5 - Recetas

1. Listar receta guardada.
2. Abrirla.
3. Editar nombre/descripcion.
4. Duplicar.
5. Borrar copia.
6. Exportar JSON.

Estado: documentado, pendiente de ejecucion manual.

### Smoke 6 - AFIP/ARCA inicial

Instruccion:

```text
Entra a AFIP/ARCA y toca iniciar sesion. Cuando pida credenciales, pausa y avisame.
```

Esperado:

* resolucion por target generico
* no hardcode AFIP/ARCA
* pausa humana ante credenciales/captcha/2FA
* resume reobserva pagina

Estado: documentado, pendiente de ejecucion manual.

## Limitaciones abiertas

* Learning/Recipes es V0 local.
* Replay de recetas usa instruccion al operador, no un runner deterministico paso-a-paso.
* No hay suite JS con fixtures browser automatizados todavia.
* La UI esta lista para diagnostico y operacion inicial, pero no tiene perfiles usuario/tecnico separados.

## Proximo hito recomendado

`NEXA Browser-003 - Deterministic Recipe Runner + JS Fixture Tests`

Enfocado en:

* runner deterministico de recetas
* fixtures HTML automatizados
* validacion de learning event payloads
* replay paso-a-paso con checks por target
