# HITO-142 - Safe Executor Surface

## Objetivo

Ampliar `UiaPatternExecutor` desde `Button` solamente hacia una superficie segura basada en allowlist de roles y un unico patron permitido:

- `Button`
- `Hyperlink`
- `MenuItem`
- `InvokePattern` solamente

Todo queda fail-closed:

- sin fallback
- sin `el.Click`
- sin `GetClickablePoint`
- sin `SendInput`
- sin coordenadas

## Inventario previo

Antes de este hito:

- `UiaPatternExecutor` resolvia el target con `SelectorEngine`
- reatachaba el `AutomationElement`
- exigia `SafeRole(match) == "Button"`
- ejecutaba `match.AsButton().Invoke()`
- si el rol no era `Button`, devolvia `FailureKind.PolicyDenied`

Consumidores reales actuales:

- el smoke benigno del Pilot a traves de `PilotUiaHarnessClickExecutor`
- `SafeExecutionFsm` depende de `IUiaPatternExecutor`, pero el harness real sigue entrando por `LegacyHarnessExecutorAdapter`

Confirmaciones de alcance:

- `safe.click` no usa `UiaPatternExecutor`
- `ExecuteSafeClick` no se toca
- `UiaActionExecutor` fallback no se toca
- `ExecutorHarnessService` no se toca
- `SafeExecutionFsm` no se toca

## ExecutorSurfacePolicy

Se agrega `ExecutorSurfacePolicy` puro en Core.

Decision:

- role vacio => deny `PolicyDenied`
- role desconocido => deny `PolicyDenied`
- role fuera de allowlist => deny `PolicyDenied`
- role allowlisted sin `InvokePattern` => deny `PolicyDenied`
- role allowlisted con `InvokePattern` => allow

Razones canonicas:

- `empty role denied fail-closed`
- `role not in executor surface allowlist`
- `role allowlisted but does not support InvokePattern`

## UiaPatternExecutor antes

- `Button` solamente
- `AsButton().Invoke()`

## UiaPatternExecutor despues

- obtiene `role` con `SafeRole(match)`
- obtiene `invokeSupported` con `match.Patterns.Invoke.IsSupported`
- consulta `ExecutorSurfacePolicy.Decide(role, invokeSupported)`
- si deny:
  - devuelve `FailureKind.PolicyDenied`
  - no hace fallback
- si allow:
  - ejecuta `match.Patterns.Invoke.Pattern.Invoke()`

Garantias:

- no `AsButton`
- no `el.Click`
- no `GetClickablePoint`
- no `SendInput`
- no coordenadas
- no fallback

## Allowlist actual

- `Button`
- `Hyperlink`
- `MenuItem`

## Roles futuros

- `SplitButton`
- `ListItem`
- `TabItem`

No se habilitan todavia.

## Roles bloqueados

- `CheckBox`
- `RadioButton`
- `ComboBox`
- `Edit`
- `Document`
- `Window`
- `Pane`
- `Custom`
- `Unknown`

## Pattern permitido

- `InvokePattern` solamente

## Por que safe.click no se toca

`safe.click` sigue usando su propio camino legacy. Este hito prepara la superficie segura del executor para una migracion posterior, pero no cambia dispatch real ni retiro de `el.Click`.

## Por que el.Click no se retira todavia

Retirarlo ahora mezclaria dos cambios:

- superficie permitida del executor
- migracion real del dispatch de `safe.click`

Ese acoplamiento es innecesario. HITO-142 deja la policy segura lista. HITO-143 puede migrar dispatch con esa base ya cerrada.

## Tests

Se agregan tests puros para `ExecutorSurfacePolicy`:

- allow para `Button`, `Hyperlink`, `MenuItem`
- deny por falta de `InvokePattern`
- deny de roles bloqueados
- deny de roles futuros
- deny fail-closed para role vacio o desconocido
- matching case-insensitive

No se agregan tests UI reales nuevos. El smoke benigno historico sigue siendo la verificacion operativa del camino `Button`.

## Validacion esperada

- build verde
- tests verdes
- smoke baseline verde
- secret scan limpio

## Proximo hito

HITO-143 - safe.click FSM dispatch migration + el.Click retirement + enforcement opt-in/strict
