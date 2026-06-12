# HITO-115 - UIA CacheRequest Snapshot v1

## Objetivo

Reducir el costo de lectura UIA sin rehacer la arquitectura. El cambio introduce `CacheRequest` en el camino de snapshot/diagnostico para pedir en bloque las propiedades usadas por ONE BRAIN y evitar round-trips COM repetidos por cada propiedad/hijo.

## Alcance aplicado

- Se agrego `UiaSnapshotOptions` como configuracion interna del snapshot.
- Se agrego `UiaSnapshotPropertySet` para documentar el set minimo de propiedades cacheadas.
- Se agrego `UiaSnapshotCacheRequestFactory` para crear el `CacheRequest` de FlaUI.
- `CognitiveSnapshotReader` ahora lee desde HWND mediante `UiaElementReader.ReadFromHandleDetailed`, activando cache antes de `automation.FromHandle(hwnd)`.
- `UiaDiagnosticReader` tambien intenta cachear antes de obtener el root y cae al modo anterior si el provider UIA no soporta cache.
- `UiaTreeWalker.SafeChildren` prefiere `CachedChildren` y usa `FindAllChildren()` como fallback.
- Se mantiene la salida observable existente: mismos snapshots, mismas rutas, mismos limites y mismo fallback defensivo.

## Propiedades cacheadas

Propiedades de elemento:

- `Name`
- `AutomationId`
- `ControlType`
- `ClassName`
- `BoundingRectangle`
- `IsEnabled`
- `IsOffscreen`
- `HasKeyboardFocus`
- `IsKeyboardFocusable`
- `HelpText`

Disponibilidad de patrones:

- `IsInvokePatternAvailable`
- `IsValuePatternAvailable`
- `IsTogglePatternAvailable`
- `IsSelectionItemPatternAvailable`
- `IsLegacyIAccessiblePatternAvailable`
- `IsTextPatternAvailable`

Propiedades de patrones ya usadas:

- `LegacyIAccessible.Name`
- `LegacyIAccessible.Value`
- `Value.Value`

## Por que reduce round-trips

Antes, el walker leia propiedades directamente sobre cada `AutomationElement` y obtenia hijos con `FindAllChildren()` en cada nodo. En UIA eso puede disparar llamadas COM individuales por propiedad y por paso del arbol.

Con `CacheRequest`, FlaUI pide el subtree y las propiedades relevantes en una sola lectura cacheada cuando el provider lo soporta. Despues, el walker puede reutilizar `CachedChildren` y propiedades ya solicitadas. Si el cache falla o el provider no lo soporta, el codigo vuelve al camino anterior.

No se agrega un test de performance porque seria dependiente del estado del escritorio, apps abiertas y provider UIA. El test fija configuracion/propiedades y la validacion de build/tests asegura compatibilidad.

## Safety

Este hito no cambia comportamiento de acciones:

- no habilita clicks
- no cambia playback
- no llama OpenAI
- no toca API keys
- no acepta cookies
- no hace login
- no carrito
- no compra
- no pago
- no auto-open de HTML/artifacts

El cambio queda acotado a lectura UIA de snapshot/diagnostico y conserva fallback fail-safe al modo uncached.

## Validacion esperada

- build OK
- tests OK
- `NEGATIVE_EXIT_CODE=1`
- demo script OK
- demo Markdown dry-run/run OK
- demo HTML dry-run/run OK
- ML readonly/diagnostic OK
- `artifacts/` ignorado
- `git status --short` limpio

## Pendiente futuro

- Identidad estable por `RuntimeId` o equivalentes seguros.
- Waits por eventos UIA.
- Medicion comparativa controlada de performance.
- Politica de snapshot por AppProfile si se detectan providers problematicos.
