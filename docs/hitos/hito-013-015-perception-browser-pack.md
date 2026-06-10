# HITO-013+015 — Perception + Browser Automation Pack

## 1. Objetivo del bloque doble

Mejorar percepción + automatización browser autorizada para que ONE BRAIN pueda leer páginas web simples de forma más robusta, sin depender exclusivamente de UIA textual.

Combina:
- **HITO-013** — Perception Upgrade Pack
- **HITO-015** — Browser Automation Pack

Con alcance controlado: sin OCR pesado, sin CDP/DevTools complejo, sin bypass anti-bot.

## 2. Qué se agregó

### Cancelación cooperativa
- Campo `_stepDeadlineTicks` en RecipeRunner
- Deadline se calcula antes de cada step (`Stopwatch.GetTimestamp() + stepTimeout`)
- Helper `IsStepExpired()` para chequeo rápido
- Los 3 loops críticos verifican `IsStepExpired()` en cada iteración:
  - `ExecuteBrowserOpen` (polling de ventana)
  - `ExecuteWait` (polling UIA snapshot)
  - `ExecuteSnapshotRead` (polling de elemento)
- El wrapper `Task.Run + Wait(timeout)` se mantiene como cinturón de seguridad

### Browser read (vía UIA)
- Nuevo step kind: `browser.read`
- Propiedades soportadas: `title`, `text`, `url`
- Lee snapshot UIA del proceso browser especificado
- Extrae title del `WindowSnapshot.Title`
- Extrae url buscando Edit controls con patrones http
- Extrae text de nombres de elementos visibles (hasta 30 distintos)
- Guarda resultado en variable `{saveAs}` y `{saveAs}.raw`

### Visual capture unificado
- Nuevo step kind: `visual.capture`
- Modos: `window`, `region`, `element`, `fullscreen`
- Guarda metadata en variables:
  - `{saveAs}` = output path
  - `{saveAs}.path` = output path
  - `{saveAs}.width` = ancho en px
  - `{saveAs}.height` = alto en px
  - `{saveAs}.timestamp` = UTC ISO timestamp
- Mantiene compatibilidad con `visual.capture.window` y `visual.capture.element`

### Recetas nuevas
1. `browser-read-example.json` — Edge + leer title + assert Example
2. `browser-read-wikipedia.json` — Wikipedia Automation page + leer title
3. `visual-browser-capture.json` — Edge + capturar ventana + verificar path
4. `browser-timeout-hard-negative.json` — timeout controlado <5s imposible

## 3. Qué NO se agregó
- Planner IA
- Recorder
- CAPTCHA bypass
- Stealth browser
- Evasión anti-bot
- Compras/pagos/envíos
- Login a sitios reales
- Control remoto externo
- OCR pesado (sin paquetes externos)
- CDP/DevTools (optado por UIA)
- Cambios al target framework

## 4. Contrato JSON: browser.read

```jsonc
{
  "kind": "browser.read",
  "process": "msedge",           // required: process name
  "window": null,                 // optional: window title filter
  "property": "title|text|url",  // default: "title"
  "saveAs": "browser.title",     // variable name to store result
  "timeoutMs": 10000             // default: 10000
}
```

## 5. Contrato JSON: visual.capture

```jsonc
{
  "kind": "visual.capture",
  "args": {
    "mode": "window|region|element|fullscreen"  // default: "window"
  },
  "process": "msedge",           // required for window/region
  "name": null,                  // element selector (region mode)
  "role": null,                  // element selector (region mode)
  "automationId": null,          // element selector (region mode)
  "out": "artifacts/recipes/capture.png",  // output path
  "saveAs": "visual.myCapture",  // variable prefix for metadata
  "timeoutMs": 10000             // default: 10000
}
```

## 6. Timeouts / cancelación

| Kind | Default timeout |
|---|---|
| `browser.open` | 15000ms |
| `browser.read` | 10000ms |
| `wait` | 10000ms |
| `visual.capture` | 10000ms |
| `delay`/`sleep` | duración + 3000ms |
| Otros | 15000ms |

Prioridad: `step.timeoutMs` > `recipe.defaultTimeoutMs` > kind-specific default.

Cooperative cancellation: deadline-based con campo `_stepDeadlineTicks`, loops revisan `IsStepExpired()`.

## 7. Recetas nuevas

| Receta | Pasos | Resultado |
|---|---|---|
| `browser-read-example.json` | 5 | PASS — lee title "example.com..." |
| `browser-read-wikipedia.json` | 5 | PASS — lee title "Automation - Wikipedia..." |
| `visual-browser-capture.json` | 5 | PASS — captura 1382x736, guarda metadata |
| `browser-timeout-hard-negative.json` | 2 | FAIL controlado (~2s) |

## 8. Validación

- Build: 0 errors, 0 warnings
- 6 recetas existentes: todas PASS
- edge-example-smoke: PASS (5022ms)
- real-smoke-windows-pack: PASS (25120ms)
- browser-timeout-negative: FAIL controlado (2020ms)
- browser-read-example: PASS (4940ms)
- browser-read-wikipedia: PASS (7470ms)
- visual-browser-capture: PASS (4699ms)
- browser-timeout-hard-negative: FAIL controlado (2010ms)
- Safety Close: bloqueado
- Sin procesos OneBrain.Cli residuales
- Sin necesidad de cerrar Edge manualmente

## 9. Limitaciones conocidas

- `browser.read` con `property: url` depende de que Edge exponga el address bar como Edit control accesible. Puede fallar si UIA no alcanza a leerlo.
- `browser.read` con `property: text` solo recolecta `Name` de elementos UIA, no el texto HTML real de la página.
- No hay OCR — la lectura de texto depende enteramente de UIA.
- El patrón `Task.Run + Wait(timeout)` puede dejar threads UIA corriendo brevemente tras timeout. Mitigado con `IsStepExpired()` en los loops internos.
- Wikipedia carga en ~7s con buena conexión; si la red está lenta, podría exceder el timeout.

## 10. Deuda técnica restante

```
Debt: current step timeout uses Task.Run + Wait(timeout); internal UIA/browser operations
do not yet consume CancellationToken. Improved with cooperative deadline checks
(IsStepExpired) in HITO-013, but full CancellationToken plumbing remains for HITO-016.

Debt: browser.read text extraction is limited to UIA element names. Full web content
reading would require CDP/DevTools integration (deferred to future hito).

Debt: visual.capture region mode requires element selector. Auto-element discovery
via visual analysis not yet implemented.
```

## 11. Próximo bloque recomendado

**HITO-014+016 — Recorder + Safety Approval Pack**
- Recorder de acciones UIA/browser
- Approvals visuales para acciones peligrosas
- Sandbox de recetas grabadas
- Mejora de safety policies
- OCR ligero si hay librería disponible
