# HITO-137 - Web Target Selector Bridge

## Objetivo

Conectar `WebTargetResolver` con el `SelectorEngine` generalizable en modo shadow read-only para medir paridad sin cambiar la seleccion real de `safe.click`.

## Flujo actual de `safe.click`

1. `RecipeRunner.ExecuteSafeClick` valida target, preflight y approval binding.
2. Si hay browser owned, llama a `WebTargetResolver.Resolve(sessionHwnd, targetText, proc)`.
3. `WebTargetResolver` enumera top-level HWNDs y child HWNDs del browser.
4. Sobre cada HWND hace probe UIA y arma `CandidateInfo` cuando `Name` coincide por igualdad o `Contains`.
5. La resolucion legacy:
   - `0` candidatos => `not found`
   - `>1` candidatos => si hay un unico candidato `enabled && !offscreen`, lo elige; si no, devuelve `ambiguous`
   - `1` candidato => success si es actionable
6. `RecipeRunner.SetResolutionVars` expone evidencia de resolucion en variables.
7. Si la resolucion fue success, `RecipeRunner` vuelve a buscar el elemento por nombre y ejecuta el click real existente.

## Que hace `WebTargetResolver` hoy

- enumera HWNDs del browser y sus children
- crea `UIA3Automation` efimera por HWND
- recorre el arbol UIA completo
- filtra por `Name`
- guarda `CandidateInfo` con:
  - `Name`
  - `ControlType`
  - `AutomationId`
  - `BoundingRect`
  - `IsEnabled`
  - `IsOffscreen`
  - `HasInvoke`
  - `HasClickablePoint`
  - `Hwnd`

## Datos disponibles y faltantes

### Datos presentes en `CandidateInfo`

- `Name`
- `ControlType`
- `AutomationId`
- `BoundingRect`
- `IsEnabled`
- `IsOffscreen`
- `HasInvoke`

### Datos que faltan para una `ElementIdentity` fuerte

- `RuntimeId`
- `AncestorPath`
- `ParentFingerprint`
- `SiblingIndex`
- `WindowTitle` estable por candidato
- identidad aprobable reutilizable

Por eso la identidad web en este hito sigue siendo debil.

## Por que este hito es shadow/read-only

- `safe.click` sigue usando approval string-based.
- `safe.click` sigue dependiendo de `WebTargetResolver`.
- el executor web no esta migrado a `SelectorEngine`.
- no hay identity-at-approval web.

Cambiar seleccion real ahora mezclaria selector, approval y dispatch en una sola rebanada. Este hito solo agrega observabilidad de paridad.

## Que se construyo

En Core puro:

- `WebCandidate`
- `WebCandidateMapper`
- `WebSelectorParity`
- `WebSelectorBridge`

Comportamiento:

- convierte candidatos web a `ElementIdentity` debil
- construye selector name-based o usa `SelectorDefinition` explicita
- ejecuta `SelectorEngine.Resolve`
- calcula:
  - `ShadowEngineFound`
  - `ShadowEngineVerdict`
  - `ShadowEngineSelectedName`
  - `ShadowAgreesWithLegacy`
  - `ShadowReasons`

## Integracion aplicada

`WebTargetResolver.Resolve` ahora:

- conserva el camino legacy intacto
- despues de recolectar candidatos ejecuta `WebSelectorBridge` en paralelo logico
- proyecta el resultado shadow sobre `WebTargetResult` con campos aditivos
- si el bridge falla, no rompe la resolucion legacy y registra `ShadowUnavailable`

`RecipeRunner.SetResolutionVars` ahora expone variables aditivas:

- `*.resolution.shadow.found`
- `*.resolution.shadow.verdict`
- `*.resolution.shadow.agreesWithLegacy`
- `*.resolution.shadow.selectedName`
- `*.resolution.shadow.reasons`

## Que NO se hizo

- no `safe.click` migration
- no approval changes
- no executor changes
- no FSM changes
- no clicks
- no `SendInput`
- no OCR / vision / CDP
- no `RegionSelector.FindBestMatch`
- no `BasicActionVerifier.TargetExists`

## Legacy absorbido

- ninguno en modo operativo
- solo se agrega bridge shadow sobre `WebTargetResolver`

## Legacy pendiente

- HITO-138: identity-at-approval web
- HITO-139: executor surface + safe.click FSM migration
- `RegionSelector.FindBestMatch`
- `BasicActionVerifier.TargetExists`

## Fixtures creadas

- `tools/fixtures/web-parity/single-candidate.json`
- `tools/fixtures/web-parity/duplicate-equivalent-candidates.json`
- `tools/fixtures/web-parity/automation-id-strong.json`
- `tools/fixtures/web-parity/empty-candidates.json`
- `tools/fixtures/web-parity/disabled-offscreen-candidates.json`
- `tools/fixtures/web-parity/weak-identity-no-runtimeid.json`

## Tests

- paridad con candidato unico
- ambiguedad sin `first-wins`
- soporte de `automation-id` cuando el selector lo explicita
- `not found`
- identidad web debil nunca reporta `Same`
- razones para candidatos disabled/offscreen
- malformed input fail-closed
- shadow projection sin mutar campos legacy del resultado

## Validaciones

- `dotnet build OneBrain.slnx`
- `dotnet test OneBrain.slnx`
- smoke read-only de baseline diagnostic
- secret scan del diff con patrones de alta confianza
- `artifacts/` ignorado/no trackeado
