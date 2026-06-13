# HITO-134 — Generalizable Target Resolver

## Objetivo

Construir el resolver generalizable real de ONE BRAIN para desktop/UIA sin cambiar safety ni ampliar capacidades de acción:

- ElementIdentity v2
- ElementFingerprintBuilder
- ElementMatcher con veredictos explicables
- SelectorDefinition / SelectorResolution / SelectorEngine
- fixtures de paridad para casos históricos
- absorción inicial de resolvers duplicados en UIA executor y executor harness

## Inventario previo

### Resolvers y matching encontrados

1. `src/OneBrain.Actions/Uia/UiaActionExecutor.cs`
   - `ResolveTarget`
   - `BestNameMatch`
   - `RankForType`
   - `RankForInvoke`
   - lógica histórica de matching por `name`, `help`, `legacy`, `labeledBy`

2. `src/OneBrain.Core/ExecutorHarness/ExecutorHarnessTargetResolver.cs`
   - resolución allowlisted del target benigno local
   - validaciones de harness/appProfile/window/target/action kind

3. `src/OneBrain.Observation/Visual/RegionSelector.cs`
   - `FindBestMatch`
   - resolver visual/diagnóstico para captura de regiones

4. `src/OneBrain.Cli/Safety/WebTargetResolver.cs`
   - resolver web/browser separado

5. `src/OneBrain.Verification/Engine/BasicActionVerifier.cs`
   - matching liviano de existencia post-acción por selector legacy

### Qué se absorbe en este hito

- `UiaActionExecutor.ResolveTarget` deja de rankear por cuenta propia y pasa a delegar a `SelectorEngine`.
- `ExecutorHarnessTargetResolver` pasa a usar `SelectorEngine` para la parte de identidad del target allowlisted.

### Qué queda pendiente

- `WebTargetResolver`
  - pendiente por pertenecer a la rebanada web y porque no comparte todavía el mismo shape de candidatos desktop.
- `RegionSelector.FindBestMatch`
  - pendiente; hoy sigue siendo resolución visual/diagnóstica y no forma parte del camino de ejecución UIA/harness.
- `BasicActionVerifier.TargetExists`
  - pendiente; es verificación post-acción legacy y no bloquea la absorción inicial del resolver.

### Casos históricos de paridad preservados en este hito

- `role:name`
- `automation-id`
- `name` con señales auxiliares (`help`, `legacy`, `labeledBy`)
- `Edit` vs `Text` label
- `ONE Brain Search`
- botones `Close/Cerrar`
- nombre es-ES vs en-US con `automationId` fuerte
- recreación de elemento con `runtimeId` nuevo pero identidad débil fuerte

### Qué no se toca

- `WebTargetResolver`
- `RegionSelector.FindBestMatch`
- FSM / execution safety
- Safe Executor Contract
- approval binding
- OCR / vision / CDP

## Qué se construyó

### ElementIdentity v2

Se extendió el modelo existente `OneBrain.Core.Models.ElementIdentity` para agregar:

- `HelpText`
- `LegacyName`
- `LegacyValue`
- `LabeledByName`
- `ControlType`
- `ClassName`
- `FrameworkId`
- `ProcessName`
- `WindowTitle`
- `AncestorPath`
- `SiblingIndex`
- `ParentFingerprint`
- `BoundsHint`
- `Provenance`
- `SchemaVersion`

Se mantuvo compatibilidad con el constructor histórico de cuatro argumentos:

- `RuntimeId`
- `Role`
- `Name`
- `AutomationId`

### Fingerprint y matcher

- `ElementFingerprintBuilder`
- `ElementMatchVerdict`
- `ElementMatchResult`
- `ElementMatcher`

Reglas aplicadas:

- `RuntimeId` igual fuerte => `Same`
- identidad débil fuerte con `RuntimeId` recreado => `LikelySame`
- sin candidatos => `Stale` o `Unknown`
- ambigüedad equivalente => `Ambiguous`
- no hay `first-wins`

### Selector engine

Se agregaron:

- `SelectorDefinition`
- `SelectorResolution`
- `SelectorEngine`

Soporta parse de selectores legacy usados hoy en el repo:

- `role:x`
- `name:y`
- `automation-id:z`
- `class:c`
- combinaciones `role:Edit|name:ONE Brain Search`
- combinaciones con `+` ya presentes en paths legacy

### Fixtures de paridad

Se agregaron fixtures en `tools/fixtures/trees/parity/`:

- `edit-vs-text-label.json`
- `duplicate-buttons.json`
- `automation-id-strong.json`
- `close-cerrar-dangerous.json`
- `recreated-runtime-id.json`

Estos fixtures se cargan con `FixtureSensor`, sin UIA real ni Windows real.

## Qué legacy se absorbió

### Absorbido

- `UiaActionExecutor`
  - el método legacy conserva la interfaz pública y sus mensajes
  - la resolución interna ahora:
    - parsea selector legacy
    - adapta expectativas por `actionKind`
    - construye candidatos `ElementIdentity`
    - delega a `SelectorEngine`

- `ExecutorHarnessTargetResolver`
  - sigue siendo owner de política allowlisted del harness
  - la identidad del target benigno se resuelve vía `SelectorEngine`
  - quedó documentado como adapter temporal, no resolver autónomo

### No absorbido todavía

- `WebTargetResolver`
- `RegionSelector.FindBestMatch`
- `BasicActionVerifier.TargetExists`

## Por qué no se toca WebTargetResolver todavía

Porque el alcance de HITO-134 es desktop/UIA/harness con paridad del camino ya existente. El resolver web sigue otra forma de observación y selección. Moverlo ahora mezclaría la rebanada desktop con una rebanada web no estabilizada todavía.

## Por qué no hay FSM ni cambios de safety

El objetivo del hito es solo resolución de identidad/selector. No se cambian:

- políticas de safety
- approval flow
- exit codes
- capacidades de acción
- comportamiento comercial

Resolver identidad no debe implicar elevar safety ni habilitar acciones nuevas.

## Tests agregados

- matching fuerte por `RuntimeId`
- `LikelySame` con `RuntimeId` recreado
- `Stale` / `Unknown` sin candidatos
- ambigüedad sin `first-wins`
- `automation-id` fuerte aunque cambie el nombre
- `Close/Cerrar` resuelto sin convertirlo en safe
- parse de selectores legacy
- `ONE Brain Search` resuelto al `Edit` esperado
- harness resolver sigue scoped al target benigno via `SelectorEngine`

## Validaciones

- `dotnet build OneBrain.slnx`
- `dotnet test OneBrain.slnx`
- smoke read-only de baseline diagnostic
- secret scan del diff del hito con patrones de alta confianza
- `artifacts/` ignorado/no trackeado

## Riesgos pendientes

- `WebTargetResolver` sigue fuera del engine generalizable
- `RegionSelector.FindBestMatch` sigue con matching propio
- `BasicActionVerifier.TargetExists` sigue usando matching legacy simple
- no se integró todavía un contrato formal compartido para web/visual/desktop

## Próximo paso recomendado

HITO-135 — Safe Executor Contract, pero solo después de cerrar y publicar HITO-134.
