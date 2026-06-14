# HITO-160 - Legacy Quarantine / safe path locks

## Objetivo

HITO-160 bloquea por default las superficies legacy de receta que todavia podian alcanzar escritura/click/teclado fuera del patron seguro:

* `actv.type`
* `actv.invoke`
* `key`

El hito no refactoriza `RecipeRunner` en handlers nuevos y no implementa acciones nuevas.

## Inventario legacy

Inventario estatico encontrado:

* `RecipeRunner.ExecuteActvType` llamaba `BasicActionVerifier.ExecuteAndVerify`.
* `RecipeRunner.ExecuteActvInvoke` llamaba `BasicActionVerifier.ExecuteAndVerify`.
* `RecipeRunner.ExecuteKey` llamaba `UiaActionExecutor.Execute`.
* `UiaActionExecutor` sigue existiendo como superficie legacy; internamente puede usar paths historicos como click/fallback.
* `BasicActionVerifier` sigue existiendo como verificador legacy para `actv.*`.
* `WebTargetResolver.SafeClickable` usa `GetClickablePoint` como helper legacy de observacion, no como executor `safe.*`.
* `ExecuteSafeClickLegacy` sigue compilado pero no tiene callers desde `safe.click` y retorna por la policy de retirement antes del cuerpo legacy.
* `Program.cs` aun contiene comandos historicos que construyen pasos legacy; H160 no reescribe la CLI.

## LegacyExecutionGuard

Se agrega:

```text
LegacyExecutionGuard
LegacyExecutionDecision
```

Reglas:

* deny-by-default;
* `safe.*` nunca puede habilitar legacy aunque exista opt-in;
* superficies desconocidas bloquean;
* solo legacy step kinds conocidos pueden evaluar opt-in;
* opt-in requiere las dos condiciones:
  * variable de entorno `ONEBRAIN_ALLOW_LEGACY_ACTIONS=1`;
  * argumento de receta `allowLegacyActions=true` o `allowLegacyActions=1`.

El opt-in es dev/test/compatibilidad, explicito y auditable.

## RecipeRunner

Antes de cualquier accion real:

* `actv.type` llama `LegacyExecutionGuard`.
* `actv.invoke` llama `LegacyExecutionGuard`.
* `key` llama `LegacyExecutionGuard`.

Si el guard bloquea:

* no se llama `BasicActionVerifier`;
* no se llama `UiaActionExecutor`;
* no se activa ventana;
* no se envia teclado;
* no se toca UI;
* el step retorna `Success=false`;
* se escriben variables `legacy.*` y `{saveAs}.legacy*`.

## Variables legacy

Variables globales:

* `legacy.success=false`
* `legacy.blocked=true`
* `legacy.stepKind`
* `legacy.surface`
* `legacy.reason`
* `legacy.optInRequired=true`
* `legacy.guard.allowed=false`
* `legacy.guard.isQuarantined=true`

Variables por step cuando hay `saveAs`:

* `{prefix}.legacyBlocked=true`
* `{prefix}.success=false`
* `{prefix}.failureKind=PolicyDenied`
* `{prefix}.reason`
* `{prefix}.legacy.stepKind`
* `{prefix}.legacy.surface`
* `{prefix}.legacy.guard.allowed=false`

## safe.* locks

`safe.click`, `safe.read` y `safe.type` quedan fuera del opt-in legacy.

Source-scan locks cubren que los handlers safe no alcancen:

* `UiaActionExecutor`
* `BasicActionVerifier`
* `ExecuteSafeClickLegacy`
* `actv.type`
* `actv.invoke`
* `SendInput`
* `SendKeys`
* `KeyboardInput`
* `keybd_event`
* `SetCursorPos`
* `mouse_event`
* `Clipboard`
* `GetClickablePoint`
* `el.Click`
* `.Click(`

## ExecuteSafeClickLegacy

`ExecuteSafeClickLegacy` permanece aislado y bloqueado por la policy de retirement existente.

No tiene callers desde `safe.click`.

## UiaActionExecutor y BasicActionVerifier

Ambos quedan documentados en codigo como legacy/quarantine.

No se agrega `[Obsolete]` para evitar warnings de build mientras existan referencias legacy protegidas por guard.

`BasicActionVerifier.TargetExists` no se modifica.

## Que NO cambia

* no cambia `safe.click`;
* no cambia `safe.read`;
* no cambia `safe.type`;
* no cambia approval-v2;
* no cambia `evidenceHash`;
* no cambia `policyVersion`;
* no cambia `ValidateApprovalBinding`;
* no cambia `RegionSelector.FindBestMatch`;
* no cambia `BasicActionVerifier.TargetExists`;
* no hay DOM/CDP;
* no hay OCR;
* no hay WGC/DXGI;
* no hay refactor grande de `RecipeRunner`;
* no hay identity/fingerprint v2;
* no hay wait engine.

## Proximos hitos

* H161 - Approved-Input Binding Unification
* H162 - Identity/Fingerprint v2
* H163 - UIA truncation detection
* H164 - Wait engine declarativo
