# HITO-161 - Approved-Input Binding Unification

## Objetivo

H161 cierra la deuda M-1 detectada tras H159: `safe.type` ya no autoriza escritura usando una variable runtime suelta como `approval.input.approvedTextDigest`.

El texto aprobado queda ligado al manifest v3 mediante un binding versionado:

```text
approved input binding =
  actionKind
  + approvalRef
  + identityBindingHash
  + approvedValueDigest
  + digest algorithm
  + canonicalization
```

## Modelo agregado

Se agregó:

* `ApprovedInputBinding`
* `ApprovedInputBindingHashBuilder`
* `ApprovedInputBindingValidator`

Versión:

```text
approved-input-v1
```

Digest:

```text
SHA256 / utf8-raw-v1
```

El hash unificado no incluye texto crudo. Liga identidad aprobada y valor aprobado sin exponer el input.

## Manifest v3

`approval.manifest` puede transportar de forma aditiva:

* `approvedInput.bindingVersion`
* `approvedInput.actionKind`
* `approvedInput.approvalRef`
* `approvedInput.identityBindingHash`
* `approvedInput.approvedValueDigest`
* `approvedInput.approvedValueDigestAlgorithm`
* `approvedInput.approvedValueCanonicalization`
* `approvedInput.approvedInputBindingHash`

También se exponen variables derivadas:

* `approval.input.approvedTextDigest`
* `approval.input.valueDigest`
* `approval.input.bindingHash`
* `approval.input.bindingVersion`
* `approval.input.digestAlgorithm`
* `approval.input.canonicalization`
* `approval.input.source=manifest`

Estas variables son diagnósticas. La autoridad para `safe.type` es el `manifestJson`.

## safe.type

Nuevo flujo de autorización de input:

```text
texto final resuelto
  -> SHA256
  -> approved input binding desde manifestJson
  -> ApprovedInputBindingValidator
  -> RecipeSafetyContract(ActionKind=type)
  -> TypeActionContractPolicy
  -> binding vivo de identidad
  -> UiaTypeExecutor
  -> ValuePattern.SetValue
  -> readback ValueAfter == ApprovedText
```

Bloquea fail-closed si:

* falta approved input binding;
* el digest del texto runtime no coincide con el digest aprobado;
* cambia el identity binding;
* el hash unificado no coincide;
* falta `executionAllowedInThisHito=true`;
* el contrato type no contiene `ApprovedInputBindingHash`.

## Contract policy

`TypeActionContractPolicy` ahora exige:

* `ApprovedValueDigest`
* `ApprovedInputBindingHash`
* `ApprovedInputBindingVersion`
* `ApprovedInputDigestAlgorithm`

Click y read quedan sin cambios funcionales.

## executionAllowedInThisHito

Se agregó `SafeExecutorAuthorizationPolicy`.

Política:

* `safe.type` exige `executionAllowedInThisHito=true`.
* `safe.read` también lo exige porque consume manifest/approval en el path safe executor.
* `safe.click` mantiene su gate existente.

## Legacy

`ExecuteSafeClickLegacy` sigue bloqueado/aislado por la política de retirement heredada de H154/H160. No hay camino desde `safe.click` hacia legacy.

## Qué no cambió

* No cambios breaking a approval-v2.
* No cambios a `evidenceHash`.
* No cambios a `policyVersion`.
* No cambios a `ValidateApprovalBinding`.
* No cambios a `RegionSelector.FindBestMatch`.
* No cambios a `BasicActionVerifier.TargetExists`.
* No cambios funcionales al executor de `safe.type`.
* No `SendInput`.
* No coordenadas.
* No clipboard.
* No OCR.
* No legacy safe.*.

## Validación esperada

* Build OK, 0 warnings, 0 errors.
* Tests OK.
* Smoke baseline `diagnose baseline process-not-found`.
* Secret scan sin hallazgos de alta confianza.

## Próximos hitos

* H162 - Identity/Fingerprint v2.
* H163 - UIA truncation detection.
* H164 - Wait engine declarativo.
