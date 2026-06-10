# HITO-012 — Real World Automation Pack

## Objetivo

Paquete de automatización real controlada para probar ONE BRAIN en Windows con apps reales: Calculator, Notepad,
Explorer, y Edge. Sin OCR, sin CDP, sin compras ni acciones peligrosas.

## Recetas Creadas

### 1. notepad-report-smoke.json

Abre Notepad y escribe un reporte usando variables y templates con timestamp runtime.

```powershell
dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/notepad-report-smoke.json
```

**Apps que abre:** Notepad
**Qué hace:** Escribe "ONE BRAIN HITO-012", resultado "56", y timestamp actual.
**Seguridad:** Solo escribe texto en Notepad. No guarda archivos.

### 2. explorer-temp-smoke.json

Abre Explorer en `%TEMP%` (usando `{{runtime.temp}}`) y confirma que la ventana es visible.

```powershell
dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/explorer-temp-smoke.json
```

**Apps que abre:** Explorer
**Qué hace:** Navega a `%TEMP%`. Usa delay de 2s y wait con UIA. No modifica archivos.
**Seguridad:** Solo abre carpeta temporal. No borra ni modifica archivos.

### 3. edge-example-smoke.json

Abre Edge, navega a `https://example.com`, confirma título contiene "Example".

```powershell
dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/edge-example-smoke.json
```

**Apps que abre:** Microsoft Edge
**Qué hace:** Navega a example.com. Usa delay 4s + wait por título UIA.
**Precondición:** Cerrar Edge antes de ejecutar si ya está abierto.
**Seguridad:** Solo visita example.com. No rellena formularios, no envía datos.

### 4. real-smoke-windows-pack.json

Receta integrada completa: Calculator 7x8 → Notepad report → Explorer %TEMP% → Edge example.com.

```powershell
dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/real-smoke-windows-pack.json
```

**Apps que abre:** Calculator, Notepad, Explorer, Microsoft Edge
**Qué hace:** Ejecuta secuencia completa de automatización real con variables y assertions.
**Duración esperada:** ~30s
**Seguridad:** Sin acciones destructivas. Sin formularios. Sin compras.

## Mejoras al Recipe Runner

### Runtime variables

| Variable | Valor |
|---|---|
| `runtime.timestamp` | ISO 8601 sin zona (e.g. "2026-06-10T15:00:00") |
| `runtime.date` | Fecha (e.g. "2026-06-10") |
| `runtime.temp` | Ruta expandida de `%TEMP%` |

Inyectadas automáticamente al inicio de cada receta. Disponibles via `{{runtime.timestamp}}` etc.

### Step kind: `note`

```json
{ "kind": "note", "text": "Mensaje con {{variables}}" }
```

Resuelve templates, agrega mensaje al reporte, siempre passed. Útil para debugging.

### Step kind: `delay`

```json
{ "kind": "delay", "timeoutMs": 2000 }
```

Alias de `sleep`. Pausa simple. Preferir `wait` cuando sea posible.

## Limitaciones Conocidas

- **Explorer:** Requiere delay de 2s después de abrir para que UIA tree esté disponible.
- **Edge:** Si Edge se cierra con `fresh: true`, muestra prompt "Restaurar páginas" que bloquea navegación.
  Solución: cerrar Edge manualmente antes de ejecutar, o usar `fresh: false`.
- **Notepad (Windows 11 XAML):** El área de texto no expone elemento Edit/Document por UIA. Se usa fallback
  de teclado por ventana activa.
- **No se guardan archivos, no se cierran apps al finalizar.**

## Resultados

| Receta | Resultado |
|---|---|
| calculator-to-notepad.json | 9/9 |
| calculator-to-notepad-variables.json | 11/11 |
| conditional-equals-then.json | 1/1 |
| conditional-equals-else.json | 1/1 |
| conditional-contains.json | 1/1 |
| conditional-exists.json | 2/2 |
| notepad-report-smoke.json | 4/4 |
| explorer-temp-smoke.json | 4/4 |
| edge-example-smoke.json | 4/4 |
| real-smoke-windows-pack.json | 20/20 |
| Safety (Close) | Bloqueado |

Build: 0 errors, 0 warnings.

## Próximo Paso Sugerido

**HITO-013 — App Profiles**: definir perfiles de aplicaciones (selectores, timeouts, fallbacks) para
simplificar recetas y evitar repetir `process: "ApplicationFrameHost" / window: "Calculadora"` en cada paso.
