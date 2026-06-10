# HITO-016+017 — Runtime Safety + Cancellation Hardening

## 1. Motivo del hardening

Auditoría crítica detectó:
- Safety bypass: actions kinds desconocidos caían a Invoke/Click sin evaluación
- ShouldStop no detenía con steps de kind vacío
- Deadline compartida entre steps (zombie threads)
- Sin protección pre-input físico (foreground, cancelación)
- Timeout wrapper e interno con mismo valor (carrera de mensajes)

## 2. Hallazgos corregidos

### C1 — Safety bypass por kind desconocido
**Problema:** `UiaActionExecutor.ExecuteCore` tenía fallback default: si el kind no era type/focus/key, ejecutaba `Button.Invoke()` o `Click()`. Safety solo evaluaba `"invoke"` como peligroso. `click`, `press`, `foo` bypassaban safety.

**Fix:**
- `MinimalSafetyGuard.IsPotentiallyDangerousAction` expandido a: `invoke`, `click`, `press`
- `UiaActionExecutor.ExecuteCore`: kinds explícitos requeridos (`invoke`/`click`/`press` en bloque explícito con safety check previo). Kind no reconocido devuelve `"Unsupported action kind: X"`
- Sin fallback default a Invoke/Click

### A4 — ShouldStop con kind vacío
**Problema:** Step sin kind llamaba `ShouldStop(recipe, step, forceContinueOnError)` sin `stepSuccess: false`. Con `StopOnFirstFailure: true`, la receta continuaba al siguiente step.

**Fix:** `ShouldStop` ahora recibe `stepSuccess: false` explícito para steps inválidos. Segundo step no ejecutado.

### Cancelación cooperativa per-step
**Problema:** `_stepDeadlineTicks` era campo mutable compartido. Thread zombie podía leer deadline del siguiente step.

**Fix:** Reemplazado por `CancellationTokenSource? _stepCts` por step. Cada step crea su propio CTS. El token se cancela en timeout. Los loops internos chequean `_stepCts?.IsCancellationRequested`. Thread zombie que lea CTS cancelado sale del loop.

### No physical input post-timeout
**Problema:** Thread zombie podía ejecutar Keyboard.Type/Invoke/Click después del timeout.

**Fix:** `UiaActionExecutor` chequea `request.CancellationToken.IsCancellationRequested` antes de cada acción física (Keyboard.Type, Invoke, Click). Error: `"Step expired before physical input"`.

### Foreground check antes de input sintético
**Problema:** `key` y fallback typing podían escribir en ventana equivocada si el foco cambiaba.

**Fix:** `UiaActionExecutor` guarda `expectedHwnd` después de `Activate()`. Antes de `Keyboard.Type`, verifica `GetForegroundWindow() == expectedHwnd`. Si no coincide: `"Foreground changed before input; aborting"`.

### Timeout externo con gracia
**Problema:** Timeout wrapper y timeout interno usaban el mismo valor, causando carrera.

**Fix:** `OuterTimeout = InnerTimeout + 3000ms` (constante `OuterGraceMs`). El error interno rico gana la carrera y se reporta consistentemente.

## 3. Pendiente

- BrowserSession ownership / cleanup (HITO-018)
- Full CancellationToken plumbing en toda la cadena UIA/COM (HITO-019)
- UIA COM timeouts/liveness checks (HITO-019)
- OCR ligero (futuro hito)
- El patrón `Task.Run + Wait(timeout)` sigue presente; migración a async/await con CancellationToken en siguiente refactor

## 4. Tests agregados

Proyecto: `tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj`
Framework: MSTest (net11.0-windows)

14 tests unitarios:
- Blocks_Close_AutomationId
- Blocks_CloseButton_AutomationId
- Blocks_Dangerous_Name_Cerrar
- Blocks_Dangerous_Name_Delete
- Blocks_Dangerous_Name_Pay
- Blocks_Dangerous_Name_Checkout
- Allows_Safe_Name
- Blocks_RunAsAdministrator
- Blocks_RunScript
- Blocks_Click_Kind
- Blocks_Press_Kind
- Allows_Type_Kind_On_Dangerous_Name
- Allows_Focus_Kind
- Blocks_Close_AutomationId_Only

Resultado: 14/14 PASS, 135ms

## 5. Recetas negativas

| Receta | Resultado | DurationMs |
|---|---|---|
| invalid-kind-stop-negative | FAIL (1 step, segundo no ejecuta) | 0 |
| debug-hang-timeout-negative | FAIL controlado (1000ms timeout) | 4029 |
| unsupported-action-kind-negative | FAIL (unsupported kind) | 9 |

## 6. Cómo validar

```powershell
dotnet build OneBrain.slnx  # 0 errors, 0 warnings
dotnet test                 # 14/14 pass

# Safety manual:
dotnet run --project src/OneBrain.Cli -- app open notepad
dotnet run --project src/OneBrain.Cli -- actv invoke --process Notepad --automation-id Close
dotnet run --project src/OneBrain.Cli -- act click --process Notepad --automation-id Close
dotnet run --project src/OneBrain.Cli -- act press --process Notepad --automation-id Close
dotnet run --project src/OneBrain.Cli -- act foo --process Notepad --automation-id Close
# Notepad no debe cerrarse en ningún caso
```
