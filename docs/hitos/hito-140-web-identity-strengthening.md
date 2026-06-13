# HITO-140 — Web Identity Strengthening

## Objetivo

Fortalecer la identidad web en modo aditivo y shadow-measured.

La meta de esta rebanada es hacer que `WebTargetResolver` capture campos UIA más estables y los propague por el camino:

`CandidateInfo -> WebCandidate -> WebCandidateMapper -> ElementIdentity -> WebSelectorBridge / SafeClickPlanner`

Sin cambiar:

- seleccion legacy de `safe.click`
- `Found`
- `Selected*`
- `Reason`
- `CandidateCount`
- execution/dispatch
- approval
- FSM
- enforcement

## Hallazgo previo

Antes de este hito:

- `CandidateInfo` capturaba:
  - `Name`
  - `ControlType`
  - `AutomationId`
  - `IsEnabled`
  - `IsOffscreen`
  - `HasInvoke`
  - `HasClickablePoint`
  - `BoundingRect`
  - `Hwnd`
- `WebCandidate` ya tenía `RuntimeId` desde HITO-139.
- `WebTargetResolver` vivo no llenaba `RuntimeId`.
- `EvaluateShadowParity` tampoco lo propagaba al mapear `CandidateInfo -> WebCandidate`.

Resultado:

- la identidad web real seguía siendo débil aunque el modelo ya soportaba identidad fuerte
- `SafeClickPlanner` sólo podía proyectar `Bound` cuando el test inyectaba `RuntimeId` manualmente

## Campos nuevos capturados

Se agregan de forma aditiva:

- `RuntimeId`
- `ClassName`
- `HelpText`
- `LegacyName`
- `FrameworkId`
- `AncestorPath`
- `ProcessName`
- `WindowTitle`

## Tabla de estabilidad

- `RuntimeId`: sí, cuando UIA lo expone
- `ClassName`: sí, best-effort
- `HelpText`: best-effort, sanitizado/truncado
- `LegacyName`: best-effort, sanitizado/truncado
- `FrameworkId`: best-effort
- `AncestorPath`: shallow path, hasta 4 ancestros
- `ProcessName`: estable por contexto de resolución
- `WindowTitle`: best-effort, sanitizado/truncado
- `LegacyValue`: no
- `SiblingIndex`: no
- `ParentFingerprint`: no

## Impacto en Core

`WebCandidate` y `WebCandidateMapper` ahora propagan:

- `RuntimeId`
- `ClassName`
- `HelpText`
- `LegacyName`
- `FrameworkId`
- `AncestorPath`
- `ProcessName`
- `WindowTitle`

Esto permite que:

- `ElementIdentity.IsStrong` sea verdadero cuando hay `RuntimeId`
- `ElementMatcher` pueda devolver `Same` con identidad fuerte real
- `SafeClickPlanner` pueda proyectar `Bound` si manifiesto y candidato coinciden por `RuntimeId`

## Variables aditivas

Se agregan variables:

- `*.resolution.identity.runtimeIdPresent`
- `*.resolution.identity.runtimeId`
- `*.resolution.identity.strength`
- `*.resolution.identity.ancestorPath`
- `*.resolution.identity.frameworkId`
- `*.resolution.identity.className`
- `*.resolution.identity.helpTextPresent`
- `*.resolution.identity.legacyNamePresent`

`HelpText` y `LegacyName` no se exponen completos en variables de receta; sólo presencia booleana.

## Qué no se hizo

- no `safe.click` migration
- no execution changes
- no approval changes
- no FSM changes
- no `UiaPatternExecutor` changes
- no `el.Click` changes
- no clicks
- no enforcement

## Próximo paso recomendado

HITO-141 — Safe Executor Surface + `el.Click` retirement + approval-time identity capture
