# HITO-159 - safe.type UIA ValuePattern

## Objetivo

HITO-159 agrega la primera accion de escritura segura del motor:

```text
safe.type
```

El alcance es deliberadamente estrecho: escribir texto aprobado usando solamente UIA `ValuePattern.SetValue`.

No implementa `safe.select`, `safe.form.fill`, submit, DOM/CDP, OCR, vision, WGC/DXGI, UI comercial, API/MCP ni memoria de procesos.

## Flujo seguro

```text
target.observe / target.observe.desktop
    -> approval.manifest v3 strong con valor aprobado
    -> safe.type
    -> RecipeSafetyContract(ActionKind=type)
    -> TypeActionContractPolicy
    -> ApprovalBindingValidator contra identidad viva
    -> ownership pre-commit
    -> UiaTypeExecutor
    -> InvokeTimeIdentityGate == Same
    -> TypeSurfacePolicy
    -> ValuePattern.SetValue(ApprovedText)
    -> readback
    -> SafeTypeStepVerifier
    -> evidence ledger / safeType.*
```

## TypeActionContractPolicy

Se agrega `TypeActionContractPolicy` al `ActionContractPolicyRegistry`.

Requisitos:

* `ActionKind == type`
* `Reversible == false`
* `MaxActions == 1`
* `ApprovalRef` requerido
* `ExpectedIdentity` fuerte requerida
* `Selector` requerido
* `ActionCeiling == FullActionWithPreflight`
* `Provenance == Uia`
* `TrustLevel >= ProfileVerified`
* `ApprovedValueDigest` requerido

El registry sigue deny-by-default para acciones desconocidas.

## Valor aprobado

`safe.type` no escribe texto libre del step si no coincide con el valor aprobado.

El valor final se resuelve antes de `approval.manifest` desde el prefijo observado y se copia de forma aditiva como:

* `approval.input.approvedText`
* `approval.input.approvedTextDigest`
* `approval.input.valueDigest`

El contrato `ActionKind=type` incluye `ApprovedValueDigest`.

Antes de ejecutar, `safe.type` vuelve a calcular el digest del texto del step y bloquea si no coincide con el digest aprobado.

## Binding contra identidad viva

`safe.type` no usa el patron tautologico `candidates = [ExpectedIdentity]`.

Antes del executor:

1. resuelve el target vivo;
2. construye `ObservedIdentity`;
3. valida `ApprovalBindingValidator` contra esa identidad viva;
4. bloquea si el resultado no es `Same`.

El executor repite `InvokeTimeIdentityGate` justo antes de `SetValue`.

## TypeSurfacePolicy

`TypeSurfacePolicy` es independiente de `ExecutorSurfacePolicy` y `ReadSurfacePolicy`.

Permitido:

* roles `Edit` y `Document`
* `ValuePattern.SetValue`

Bloqueado:

* password fields
* controles sin ValuePattern
* InvokePattern
* TogglePattern
* SelectionItemPattern
* ExpandCollapsePattern
* ScrollPattern mutante
* Button
* Hyperlink
* MenuItem
* Custom
* roles desconocidos
* disabled/offscreen si la senal esta disponible

## UiaTypeExecutor

`UiaTypeExecutor`:

* resuelve target vivo desde UIA;
* valida identidad con `InvokeTimeIdentityGate`;
* valida superficie con `TypeSurfacePolicy`;
* bloquea password fields;
* lee valor previo;
* verifica ownership inmediatamente antes de commit;
* ejecuta `ValuePattern.SetValue(ApprovedText)`;
* lee valor posterior;
* reporta success solo si `ValueAfter == ApprovedText`.

No usa:

* SendInput
* SendKeys
* KeyboardInput
* keybd_event
* SetCursorPos
* mouse_event
* coordenadas
* GetClickablePoint
* InvokePattern
* click
* el.Click
* UiaActionExecutor
* BasicActionVerifier
* clipboard
* OCR
* fallback visual
* fallback silencioso

## SafeTypeStepVerifier

El verifier exige:

* dispatch success;
* identidad observada presente;
* verdict `Same`;
* invoke-time identity checked;
* ownership checked y allowed;
* surface allowed;
* mutation observed;
* `ValueAfter == ApprovedText`;
* ledger/evidencia presente en el path de receta.

No usa el modelo de `ObservedActions == 1` de click.

## Variables safeType.*

Variables principales:

* `safeType.success`
* `safeType.valueBefore`
* `safeType.valueAfter`
* `safeType.approvedTextDigest`
* `safeType.patternUsed`
* `safeType.failureKind`
* `safeType.reason`
* `safeType.identity.verdict`
* `safeType.identity.expectedDigest`
* `safeType.identity.observedDigest`
* `safeType.surface.allowed`
* `safeType.surface.reason`
* `safeType.ownership.checked`
* `safeType.ownership.allowed`
* `safeType.mutationObserved`
* `safeType.evidence.ledgerJson`
* `safeType.evidence.transitionCount`

## Tests y locks

Se agregan tests para:

* `TypeActionContractPolicy`
* `TypeSurfacePolicy`
* `SafeTypeStepVerifier`
* `safe.type` en `RecipeRunner`
* texto aprobado ligado por digest
* binding contra resolucion viva
* invoke-time gate posterior al binding
* bloqueo de `dispatchPath=legacy`
* source-scan contra fallbacks de teclado, mouse, clipboard, OCR y legacy

## Invariantes preservadas

* no cambios a approval-v2
* no cambios a `evidenceHash`
* no cambios a `policyVersion`
* no cambios a `ValidateApprovalBinding`
* no cambios a `RegionSelector.FindBestMatch`
* no cambios a `BasicActionVerifier.TargetExists`
* `safe.click` intacto
* `safe.read` intacto
* no fallback silencioso
* no clicks reales en tests

## Proximos hitos esperados

* H160 - Legacy quarantine para `actv.*` / `key`
* H161 - identity/fingerprint v2
* H162 - UIA truncation detection
* H163 - wait engine declarativo
