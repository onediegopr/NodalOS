# HITO-116+117 - RuntimeId / Stable Element Identity + Wait Events v0

## Objetivo

Agregar identidad estable minima para elementos UIA y waits event-driven en casos confiables, sin cambiar executor, playback ni safety.

Este hito no introduce Engine, SemanticScene completa, CDP, WGC, playback real ni clicks reales.

## HITO-116 - RuntimeId / Stable Element Identity

### Agregado

- `UiElementSnapshot` incluye `RuntimeId` con default `""`.
- `UiaTreeWalker.SafeRuntimeId(element)` lee `RuntimeId` de forma segura.
- `UiaTreeWalker.FormatRuntimeId(int[]?)` convierte `int[]` a string canonico con puntos, por ejemplo `42.66818.4.13`.
- `UiaTreeWalker.FindByRuntimeId(root, id, maxElements, maxDepth)` permite busqueda fuerte acotada.
- `UiaSnapshotCacheRequestFactory` cachea `RuntimeId` junto con las propiedades existentes.
- `UiaElementReader` pobla `RuntimeId` en snapshots.
- `UiaDiagnosticReader` muestra `RuntimeId` en diagnostico.
- `ElementIdentity` en Core define identidad fuerte por `RuntimeId` e identidad debil por `Role + Name + AutomationId`.

### Reglas mantenidas

- RuntimeId ausente se representa como `""`.
- RuntimeId ausente no es error.
- RuntimeId ausente no bloquea nada.
- El walker no cambia inclusion de nodos por RuntimeId.
- El executor/action layer no consume RuntimeId todavia.

### Por que RuntimeId es fuerte pero no perfecto

UIA `RuntimeId` identifica un elemento dentro del proveedor UIA actual, pero no debe asumirse persistente entre reinicios, renders completos o cambios agresivos de DOM en Chromium. Por eso se usa como identidad fuerte local cuando existe, y se conserva identidad debil como fallback descriptivo.

## HITO-117 - Wait Events v0

### Casos cubiertos

- `wait --title-contains`: intenta `PropertyChangedEvent(Name)` sobre la ventana existente.
- Aparicion de ventana: intenta `WindowOpenedEvent` sobre desktop root.
- Recipes `wait` con `titleContains` usan el mismo camino event-driven.
- Recipes/CLI `wait` con solo `process/window` esperan aparicion de ventana.
- `--poll` en CLI y `poll: true` en recipes fuerzan el camino anterior.

### Patron implementado

1. Suscribir evento.
2. Chequear condicion.
3. Esperar senal con timeout/cancellation.
4. Al senalizar, volver a verificar condicion fuera del handler.
5. Re-poll de seguridad cada 2000ms.
6. Desuscribir en `finally`.

El handler no lee UIA. Solo setea una senal.

### Fallback

Si la suscripcion falla, `UiaEventWaiter` cae silenciosamente a polling y marca:

- `UsedEvents=false`
- `FellBackToPolling=true`

Si `--poll` o `poll: true` esta activo:

- `UsedEvents=false`
- `FellBackToPolling=false`

### Casos que siguen en polling

- Wait por `name`.
- Wait por `role`.
- Wait por `automationId`.
- Waits profundos en arbol Chromium.
- StructureChanged flood.
- Playback.
- Executor/actions.

## Compatibilidad

- JSON viejo de `UiElementSnapshot` sin `RuntimeId` deserializa con `RuntimeId=""`.
- La salida de `wait` mantiene los campos existentes y agrega informacion minima:
  - `UsedEvents`
  - `FellBackToPolling`
- Si los eventos UIA no estan disponibles, el comportamiento vuelve al polling anterior.

## Safety

Este hito no cambia permisos ni acciones:

- no OpenAI real
- no API keys reales
- no playback libre
- no clicks reales
- no cookies accepted
- no login
- no carrito
- no compra
- no pago
- no auto-open de HTML/artifacts

## Pendiente futuro

- Usar RuntimeId desde executor solo cuando haya politica de revalidacion.
- Eventos para casos mas profundos si se pueden acotar sin flood.
- Waits por cambios de estructura con filtros estrictos.
- Capa futura de percepcion/semantic scene, fuera de este hito.
