# HITO-158 — safe.read + Action Contract Policy Registry + Invoke-Time Gate Testable

## Objetivo

HITO-158 agrega la primera acción nueva read-only del motor, `safe.read`, y prepara el núcleo para más acciones sin hacer crecer `ContractValidator` con ramas por `ActionKind`.

El hito cubre tres cambios controlados:

1. `ActionContractPolicyRegistry` para políticas de contrato por acción.
2. `InvokeTimeIdentityGate` puro/testeable para la compuerta de identidad justo antes del dispatch.
3. `safe.read` como acción UIA read-only, sin mutación de UI.

No implementa `safe.type`.

## Action Contract Policy Registry

Antes de este hito, `ContractValidator` contenía lógica específica para `ActionKind == "click"`.

Ahora el validador resuelve una política por acción:

```text
RecipeSafetyContract
    ↓
ContractValidator
    ↓
ActionContractPolicyRegistry.Resolve(ActionKind)
    ↓
IActionContractPolicy.Validate(...)
```

Políticas agregadas:

* `ClickActionContractPolicy`
* `ReadActionContractPolicy`
* deny-by-default para acciones desconocidas
* política explícita de harness benigno para preservar contratos de test existentes

### Política click

Se conserva la semántica de H156:

* `Reversible == false`
* `MaxActions == 1`
* `ApprovalRef` requerido
* `ExpectedIdentity` fuerte
* `Selector` requerido
* `ActionCeiling == FullActionWithPreflight`
* `Provenance == Uia`
* `TrustLevel >= ProfileVerified`

### Política read

`safe.read` se modela como una operación read-only auditada:

* `ActionKind == read`
* `Reversible == true`
* `MaxActions == 1`
* `ApprovalRef` requerido
* `ExpectedIdentity` fuerte
* `Selector` requerido
* `ActionCeiling == ReadOnly`
* `Provenance == Uia`
* `TrustLevel >= ProfileVerified`

`MaxActions == 1` significa una operación de lectura auditada, no una mutación.

## Invoke-Time Identity Gate

La compuerta invoke-time de H156 se extrajo a:

```text
InvokeTimeIdentityGate
```

Entrada:

* identidad esperada
* identidad observada

Salida:

* `Checked`
* `Allowed`
* `Verdict`
* `Reason`
* `ExpectedIdentityDigest`
* `ObservedIdentityDigest`
* `Reasons`

Reglas:

* identidad esperada requerida
* identidad esperada fuerte requerida
* identidad observada requerida
* identidad observada fuerte requerida
* sólo `Same` permite continuar
* `LikelySame`, `Different`, `Missing`, `Unknown` y casos débiles bloquean

`UiaPatternExecutor` sigue aplicando esta compuerta inmediatamente antes de `InvokePattern.Invoke()`.

## safe.read

Nuevo step:

```text
safe.read
```

Flujo:

```text
target.observe / target.observe.desktop
    ↓
approval.manifest v3 strong
    ↓
safe.read
    ↓
RecipeSafetyContract(ActionKind=read)
    ↓
ContractValidator + ReadActionContractPolicy
    ↓
ApprovalBindingValidator
    ↓
UiaReadExecutor
    ↓
SafeReadStepVerifier
    ↓
evidence ledger + variables safeRead.*
```

`safe.read` no usa `SafeExecutionFsm` de click porque su dispatch/result/verifier son read-only y no representan `ObservedActions == 1`.

## Read Surface Policy

Nueva política:

```text
ReadSurfacePolicy
```

Permite:

* `ValuePattern`
* `TextPattern`

Bloquea:

* `InvokePattern` como superficie de lectura
* patrones mutantes como `TogglePattern`, `SelectionItemPattern`, `ExpandCollapsePattern`
* targets sin patrón read-only
* rol/superficie desconocida

## UiaReadExecutor

Nuevo executor:

```text
UiaReadExecutor
```

Responsabilidades:

* resolver target por selector
* re-adjuntar identidad observada a elemento UIA vivo
* aplicar `InvokeTimeIdentityGate`
* aplicar `ReadSurfacePolicy`
* leer sólo mediante patrón read-only
* devolver identidad observada, digests y evidencia

No usa:

* `InvokePattern` para leer
* `el.Click`
* `UiaActionExecutor`
* `SendInput`
* coordenadas
* `GetClickablePoint`
* OCR
* fallback visual

## Variables agregadas

`safe.read` escribe variables bajo el prefijo del step, por defecto `safeRead`:

* `safeRead.success`
* `safeRead.value`
* `safeRead.patternUsed`
* `safeRead.failureKind`
* `safeRead.reason`
* `safeRead.identity.verdict`
* `safeRead.identity.expectedDigest`
* `safeRead.identity.observedDigest`
* `safeRead.evidence.transitionCount`
* `safeRead.evidence.ledgerJson`

## Qué NO se hizo

* no `safe.type`
* no escritura de texto
* no clicks reales
* no `InvokePattern` para `read`
* no `el.Click`
* no `UiaActionExecutor`
* no fallback silencioso
* no `SendInput`
* no coordenadas
* no `GetClickablePoint` como executor
* no OCR
* no cambios a `approval-v2`
* no cambios a `evidenceHash`
* no cambios a `policyVersion`
* no cambios a `ValidateApprovalBinding`
* no cambios a `RegionSelector`
* no cambios a `BasicActionVerifier`

## Tests

Se agregaron tests para:

* registry de políticas
* política `click` sin regresión
* política `read`
* deny-by-default
* gate invoke-time puro
* read surface policy
* verifier de `safe.read`
* integración `safe.read` con recipe y executor fake
* no uso de flujo `safe.click`

## Próximo hito

H159 — `safe.type`.

Antes de `safe.type`, mantener como regla que toda acción nueva debe tener:

```text
contrato + policy + identidad + binding + executor + verifier + evidence
```
