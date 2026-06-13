# HITO-141 — Approval-Time Identity Capture

## Objetivo

Agregar un productor read-only de identidad de target en tiempo de approval.

El objetivo es cerrar la brecha entre:

- consumidor existente: `approval.manifest` + `TryReadApprovedIdentityInput`
- productor faltante: un step que observe el target real y escriba `{prefix}.identity.*`

## Hallazgo productor/consumidor

Antes de este hito:

- `approval.manifest` ya podía consumir identidad si existía en `{prefix}.identity.*`
- `ApprovalManifestBuilder.Build(... identityInput)` ya emitía manifest v3 aditivo
- nadie producía esa identidad fuerte antes del approval
- `safe.click` sí resolvía target real, pero demasiado tarde para approval-time binding

## target.observe

Se agrega el step:

- `target.observe`

Características:

- read-only
- usa `WebTargetResolver.Resolve`
- no clickea
- no ejecuta acciones
- no toca `safe.click`
- no toca executor
- no toca approval-v2

## WebTargetResult Selected* identity fields

Se agregan de forma aditiva:

- `SelectedRuntimeId`
- `SelectedAutomationId`
- `SelectedClassName`
- `SelectedHelpText`
- `SelectedLegacyName`
- `SelectedFrameworkId`
- `SelectedAncestorPath`
- `SelectedProcessName`
- `SelectedWindowTitle`

No cambian:

- `Found`
- `SelectedName`
- `SelectedControlType`
- `SelectedBoundingRect`
- `Reason`
- `CandidateCount`

## Variables `{prefix}.identity.*`

`target.observe` escribe:

- `{prefix}.identity.runtimeId`
- `{prefix}.identity.automationId`
- `{prefix}.identity.name`
- `{prefix}.identity.role`
- `{prefix}.identity.controlType`
- `{prefix}.identity.className`
- `{prefix}.identity.frameworkId`
- `{prefix}.identity.ancestorPath`
- `{prefix}.identity.processName`
- `{prefix}.identity.windowTitle`
- `{prefix}.identity.boundsHint`
- `{prefix}.identity.provenance`
- `{prefix}.identity.source = web-uia`
- `{prefix}.identity.strength = Strong | Weak | None`
- `{prefix}.identity.helpTextPresent`
- `{prefix}.identity.legacyNamePresent`

También deja:

- `{prefix}.resolution.*`
- `{prefix}.resolution.shadow.*`
- `{prefix}.resolution.identity.*`

## Cómo approval.manifest consume identity vars

`approval.manifest` no cambió de contrato.

Sigue:

1. leyendo `evidenceJson` desde `fromPrefix`
2. leyendo identidad desde `TryReadApprovedIdentityInput(fromPrefix)`
3. construyendo manifest v3 sólo si hay metadata de identidad disponible

Con esto, una secuencia como:

- `preflight.click` con `saveAs = clickPreflight`
- `target.observe` con `saveAs = clickPreflight`
- `approval.manifest` con `from = clickPreflight`

permite emitir manifest v3 con identidad fuerte real, sin tocar approval-v2.

## Por qué es read-only

`target.observe` existe para capturar identidad aprobable, no para ejecutar.

Se clasifica como:

- `Benign`

porque:

- observa
- no escribe
- no hace click
- no hace dispatch

## Por qué no se migra safe.click

Este hito no mueve el dispatch.

`safe.click` sigue:

- con su flujo actual
- con su approval binding actual
- sin enforcement nuevo

El objetivo acá es producir mejor metadata antes del approval, no ejecutar distinto.

## Qué no se hizo

- no execution changes
- no `UiaPatternExecutor` changes
- no `el.Click` changes
- no FSM changes
- no enforcement
- no approval-v2 changes
- no `evidenceHash` changes
- no `ValidateApprovalBinding` changes

## Próximos hitos

- HITO-142 — Safe Executor Surface
- HITO-143 — safe.click FSM dispatch migration + el.Click retirement + enforcement opt-in/strict
