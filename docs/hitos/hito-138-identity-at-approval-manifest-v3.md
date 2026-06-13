# HITO-138 - Identity-at-Approval / Approval Manifest v3

## Objetivo

Extender el approval manifest con metadata de identidad del target aprobado sin cambiar enforcement, execution ni compatibilidad de `approval-v2`.

## Estado actual antes del hito

### Manifest actual

`ApprovalManifestBuilder` construye hoy un manifest textual desde preflight con:

- `policyVersion = approval-v2`
- `targetText`
- `mode`
- `decision`
- `riskCategory`
- `riskLevel`
- `executionAllowedInThisHito`
- `evidenceHash`

### Policy version actual

- `ApprovalManifestBuilder.PolicyVersion = "approval-v2"`

### Evidence hash actual

`evidenceHash` sigue siendo:

- `SHA256(targetText | mode | decision | riskCategory | riskLevel | policyVersion)`

No cambia en este hito.

### ValidateApprovalBinding actual

`RecipeRunner.ValidateApprovalBinding` sigue validando exactamente:

- `targetText`
- `mode`
- `policyVersion`
- `decision`
- `riskCategory`
- `riskLevel`
- `evidenceHash`

No usa identidad.
No se modifica en este hito.

## Por que v3 debe ser additive metadata

La identidad web actual sigue siendo debil:

- no hay `RuntimeId` estable
- no hay identity-at-approval real
- `safe.click` sigue string-based
- `approval-v2` ya esta en uso y no se puede romper

Por eso `approval-v3` se agrega como eje de metadata, no como reemplazo de `policyVersion`.

## Dos ejes de version

- `policyVersion = approval-v2`
- `identitySchemaVersion = approval-v3`

`policyVersion` sigue gobernando compatibilidad de approval y binding actual.
`identitySchemaVersion` solo describe metadata de identidad opcional.

## Que se implemento

### Modelos nuevos

En `OneBrain.Core.Approval`:

- `IdentityStrength`
- `ApprovedIdentityInput`
- `ApprovalIdentityMetadata`

### Campos nuevos en `ApprovalManifest`

- `IdentitySchemaVersion`
- `ApprovedIdentityDigest`
- `ApprovedSelector`
- `IdentityStrength`
- `IdentitySource`
- `ShadowAgreesWithLegacy`
- `IdentityBindingHash`

### Hash strategy

- `evidenceHash` no cambia
- `identityBindingHash` es separado y deterministico

`identityBindingHash`:

- `SHA256(approvedIdentityDigest | canonicalSelector | actionKind | mode | identitySource | identityStrength)`

No participa en `ValidateApprovalBinding`.

### Approval binding opcional

`ApprovalManifestBuilder.TryBuildApprovalBinding(manifest)` puede construir un `ApprovalBinding` si la metadata es suficiente, pero:

- no valida
- no bloquea
- no se usa en `safe.click`

## RecipeRunner

`approval.manifest` ahora puede consumir metadata opcional de identidad si ya viene en variables previas.

No observa UIA ni web en vivo.
No reordena el flujo.
No cambia el resultado de `safe.click`.

Variables aditivas nuevas:

- `approval.identity.schemaVersion`
- `approval.identity.digest`
- `approval.identity.strength`
- `approval.identity.source`
- `approval.identity.selector`
- `approval.identity.bindingHash`
- `approval.identity.shadowAgreesWithLegacy`
- `approval.identity.mismatch`

`approval.identity.mismatch` es solo señal no bloqueante.

## Por que no se enforcea identidad

Porque hoy faltan prerequisitos:

- identity-at-approval real para web
- executor surface segura para `safe.click`
- migracion de `safe.click` a FSM
- identidad web mas fuerte que name/string shadow

Enforcement ahora rompería compatibilidad o introduciría falsos bloqueos.

## Compatibilidad v2

- `PolicyVersion` sigue siendo `approval-v2`
- `ComputeEvidenceHash` no cambia
- `Build(preflight, mode)` sin identidad sigue produciendo el mismo manifest v2
- manifests v2 existentes siguen validos
- `ValidateApprovalBinding` no cambia

## Que NO se hizo

- no `safe.click` migration
- no execution changes
- no `approval-v2` changes
- no `ValidateApprovalBinding` changes
- no `WebTargetResolver` behavior changes
- no `UiaPatternExecutor` changes
- no FSM changes
- no clicks

## Que queda para HITO-139

- executor surface segura
- `safe.click` FSM migration
- enforcement opt-in / strict
- fortalecer identidad web
- eliminar `el.Click()` fallback si corresponde

## Validaciones

- `dotnet build OneBrain.slnx`
- `dotnet test OneBrain.slnx`
- smoke baseline read-only
- secret scan del diff
- `artifacts/` ignorado/no trackeado
