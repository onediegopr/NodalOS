# HITO-010 — Recipe Variables & Outputs

## Resumen

ONE BRAIN ahora soporta variables en recetas: capturar salida de pasos, resolver templates `{{var}}` en texto y
selectores, assertions simples, y reporte final con variables.

## Contrato JSON

### Recipe-level

| Campo | Tipo | Uso |
|---|---|---|
| `variables` | `dict<string,string>` | Carga inicial de variables (opcional) |

```jsonc
{
  "name": "my-recipe",
  "variables": { "foo": "bar" },
  "stopOnFirstFailure": true,
  "steps": [ /* ... */ ]
}
```

### Step-level (campos nuevos)

| Campo | Tipo | Kind que lo usa |
|---|---|---|
| `saveAs` | `string` | snapshot.read, wait, actv.*, visual.capture.* |
| `property` | `string` | snapshot.read |
| `transform` | `string` | snapshot.read |
| `value` | `string` | assert.equals, assert.contains |
| `expected` | `string` | assert.equals, assert.contains |
| `ignoreCase` | `bool` | assert.equals, assert.contains |

### Templates `{{variable}}`

Resueltos en: `text`, `path`, `out`, `before`, `after`, `url`, `process`, `window`, `name`, `role`, `automationId`,
`class`, `titleContains`, `value`, `expected`.

## Step Kinds Nuevos

### `snapshot.read`

Lee una propiedad de un elemento UIA y la guarda en una variable.

| Campo | Descripción |
|---|---|
| `process` | Requerido |
| `window` | Opcional |
| `name` / `role` / `automationId` / `class` | Selector (al menos uno) |
| `property` | `name`, `automationId`, `class`, `role`, `text`, `title` (default: `name`) |
| `transform` | `none`, `trim`, `calculatorResult` (default: `none`) |
| `saveAs` | Nombre de variable (requerido) |

Guarda `{saveAs}` (transformado) y `{saveAs}Raw` (valor crudo).

### `assert.equals`

| Campo | Descripción |
|---|---|
| `value` | Valor actual (soporta templates) |
| `expected` | Valor esperado |
| `ignoreCase` | Default: `false` |

### `assert.contains`

| Campo | Descripción |
|---|---|
| `value` | Valor actual (soporta templates) |
| `expected` | Substring esperado |
| `ignoreCase` | Default: `true` |

## Ejemplos

### Variable inicial

```jsonc
{
  "name": "with-initial-var",
  "variables": { "app_name": "notepad" },
  "steps": [
    { "kind": "app.open", "app": "{{app_name}}" }
  ]
}
```

### snapshot.read leyendo resultado de Calculator

```jsonc
{
  "kind": "snapshot.read",
  "process": "ApplicationFrameHost",
  "window": "Calculadora",
  "automationId": "CalculatorResults",
  "property": "name",
  "transform": "calculatorResult",
  "saveAs": "calculator.result"
}
```

Salida: `calculator.result = "56"`, `calculator.resultRaw = "La pantalla muestra 56"`.

### assert.equals

```jsonc
{
  "kind": "assert.equals",
  "value": "{{calculator.result}}",
  "expected": "56"
}
```

### assert.contains

```jsonc
{
  "kind": "assert.contains",
  "value": "{{calculator.resultRaw}}",
  "expected": "56"
}
```

### Template en text

```jsonc
{
  "kind": "actv.type",
  "process": "Notepad",
  "role": "Document",
  "text": "Resultado: {{calculator.result}}"
}
```

## Recetas de Prueba

| Archivo | Descripción | Esperado |
|---|---|---|
| `calculator-to-notepad-variables.json` | Calculator 7x8 → lee display → assert → Notepad | Success true 11/11 |
| `template-missing-variable.json` | Template con variable inexistente | Success false, error claro |
| `assert-fail.json` | assert.equals 56 = 57 | Success false, error claro |
| `assert-contains-pass.json` | assert.contains "muestra 56" contains "56" | Success true |
| `assert-contains-fail.json` | assert.contains "muestra 56" contains "99" | Success false, error claro |
| `calculator-to-notepad.json` | Receta original HITO-009 | Success true 9/9 (regresión) |

## Reglas

- Variable faltante → `RecipeVariableException` → Success false con mensaje estructurado. Nunca se reemplaza por
  vacío.
- Templates resueltos también en selectores (`name`, `role`, `automationId`, `class`).
- Safety se aplica después de resolver templates; no hay bypass.
- `snapshot.read` guarda siempre `{saveAs}` y `{saveAs}Raw`.
- `calculatorResult` es un transform opcional y declarativo; aislado en `ApplyTransform()`.

## Estado Validado

| Validación | Resultado |
|---|---|
| Build | 0 errors, 0 warnings |
| calculator-to-notepad.json | 9/9 |
| calculator-to-notepad-variables.json | 11/11 |
| template-missing-variable | Error estructurado |
| assert-fail | Error estructurado |
| assert-contains-pass | Pass |
| assert-contains-fail | Error estructurado |
| Safety regression | Bloquea "Cerrar" |
| Git | 0 commits, 0 push pendiente |

## Próximo Hito Sugerido

**HITO-011 — Conditional Steps**: branching (`if`/`else`, `skipIf`) basado en valores de variables y resultados de
assertions.
