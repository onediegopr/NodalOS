# HITO-018+019 — Browser Session Ownership + Full Cancellation Plumbing

## 1. Motivo del hito

Cerrar deuda técnica sobre sesiones de browser y cancelación profunda:
- browser.open podía matchear cualquier ventana visible de Edge, incluyendo ventanas viejas del usuario
- No había concepto de propiedad de ventana (owned vs not-owned)
- No existía browser.close seguro
- CancellationToken no llegaba a capas profundas de UIA/browser
- Sin liveness check de HWND antes de operar

## 2. Deuda técnica que corrige

- **False PASS en browser.open**: ya no usa ventana vieja. Detecta ventana nueva por diferencia de HWNDs.
- **Ownership tracking**: `browser.open` con `reuseExisting: false` (default) crea sesión owned.
- **Cleanup seguro**: `browser.close` solo cierra ventanas owned, vía WM_CLOSE.
- **Liveness check**: `IsWindow()` antes de operar sobre HWND de sesión.
- **CancellationToken**: chequeos en browser.open/close/read y visual.capture.

## 3. BrowserSession contract

Clase: `src/OneBrain.Cli/Browser/BrowserSession.cs`

```csharp
BrowserSession.Open(url, browserName, timeoutMs, token?) -> BrowserOpenResult
BrowserSession.Close(hwnd, timeoutMs) -> BrowserCloseResult
BrowserSession.IsHwndAlive(hwnd) -> bool
```

SessionInfo: `Id`, `ProcessName`, `Hwnd`, `Url`, `Owned`, `CreatedAt`

## 4. browser.open actualizado

- `reuseExisting: false` (default): lanza Edge con `--new-window`, detecta HWND nuevo por diferencia
- `reuseExisting: true`: modo legacy, encuentra cualquier ventana (no owned)
- `forceAccessibility: true`: compatible, también usa detección de ventana nueva
- Guarda variables: `{saveAs}.id`, `.process`, `.hwnd`, `.url`, `.owned`

## 5. browser.close

- Solo cierra ventanas con `owned: true`
- Usa `PostMessage(WM_CLOSE)` al HWND propio
- Espera confirmación de cierre con timeout
- Si ya está cerrada: success "already closed"
- Si no es owned: fail "not owned by this recipe"

## 6. Integración con browser.read

- Soporta `session` arg o variable de sesión via `saveAs`
- Resuelve process/hwnd desde session si existe
- Liveness check antes de leer

## 7. Cancelación/token plumbing

- `_stepCts` ya existente, propagado como **pre-check** a:
  - `ExecuteBrowserOpen` (polling loops)
  - `ExecuteBrowserClose` (antes de WM_CLOSE)
  - `ExecuteBrowserRead` (antes de snapshot)
  - `ExecuteVisualCapture` (antes de capture)
- **No** es cancelación completa (async/await).
- **No** cubre capas profundas de UIA/COM (FlaUI, COM apartment).
- `VisualCaptureService` interno y `CognitiveSnapshotReader` no reciben token.
- `Task.Run + Wait(timeout)` sigue como safety belt externo.

## 8. Liveness checks

- `BrowserSession.IsHwndAlive(hwnd)` usa `IsWindow()` P/Invoke
- Usado en browser.close, browser.read, visual.capture

## 9. Recetas nuevas

| Receta | Resultado |
|---|---|
| browser-session-example | open + read + close |
| browser-session-no-stale-window | valida no reúso de ventana vieja |
| browser-close-not-owned-negative | fail al cerrar no-owned |
| browser-session-timeout-negative | fail rápido en sesión imposible |

## 10. Limitaciones restantes

- `IsHungAppWindow` no implementado
- Refactor async/await pendiente (Task.Run + Wait sigue presente)
- UIA/COM profundo (FlaUI) no recibe CancellationToken
- `VisualCaptureService` y `CognitiveSnapshotReader` no chequean token

## 11. Próximo bloque recomendado

HITO-020+021 — Recorder + Safety Approval Pack
