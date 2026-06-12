# HITO-133 — Real App Harness Foundations

## Objetivo

Cerrar la base transversal del bloque Real App Harness sin avanzar todavía al resolver generalizable, al selector engine ni a una FSM de ejecución.

Este hito consolida:

- contratos de seguridad y procedencia
- techo de acción por fuente
- owner explícito de sesión UIA
- sensor determinista por fixture
- baseline diagnóstico read-only
- primer consumidor real de `FailureKind` vía baseline diagnostic

## Qué quedó implementado

### 133A — Core Contracts + SourceActionPolicy

Se agregaron contratos mínimos en `OneBrain.Core.Contracts`:

- `FailureKind`
- `Provenance`
- `TrustLevel`
- `ActionCeiling`
- `SourceActionPolicy`

`SourceActionPolicy.Resolve(Provenance)` aplica una política fail-closed:

- `Uia`, `Fixture`, `Api` => `FullActionWithPreflight`
- `Win32`, `Msaa`, `Dom`, `Ocr`, `Vision`, `Inferred` => `ReadOnly`
- valores inválidos => `ReadOnly`

### 133B — PerceptionSession + FixtureSensor

Se agregó `PerceptionSession` como owner explícito y reusable de `UIA3Automation`:

- `IDisposable`
- `IsDisposed`
- double-dispose seguro
- uso post-dispose bloqueado con `ObjectDisposedException`

También se agregaron overloads mínimos para reusar la sesión sin romper compatibilidad:

- `CognitiveSnapshotReader.Read(PerceptionSession, ...)`
- `CognitiveSnapshotReader.ReadFromHwnd(PerceptionSession, ...)`
- `UiaDiagnosticReader.ReadFromHandle(PerceptionSession, ...)`

Se agregó `FixtureSensor` como sensor determinista sin UI real:

- carga JSON
- valida `schemaVersion`
- exige `root`
- expone `Provenance.Fixture`
- no depende de UIA real ni de Windows real

Se agregó un fixture sample:

- `tools/fixtures/trees/notepad-sample.json`

### 133C — MetricsSnapshot + baseline diagnostic + FailureKindMapper

Se agregó:

- `MetricsSnapshot`
- `FailureKindMapper`
- `diagnose baseline --process <name> --iterations <N>`

El baseline diagnostic es read-only:

- no abre apps
- no hace clicks
- no ejecuta acciones
- mide snapshots repetidos cuando la fuente está disponible
- devuelve `process-not-found` con `FailureKind.NotFound` cuando el proceso no existe

La salida CLI del baseline ahora serializa `FailureKind` como string legible para humanos y evidence.

## Por qué existe este hito

Este hito prepara el camino vertical del Real App Harness sin activar todavía capas más costosas o invasivas:

- contratos transversales
- techo de acción por fuente
- owner explícito de UIA
- fixture sensor determinista
- baseline read-only
- primer consumidor real de `FailureKind`

La idea es estabilizar los límites del sistema antes de avanzar al resolver generalizable y a la lógica de matching.

## Qué NO se hizo

- no FSM
- no SelectorEngine
- no ElementMatcher
- no resolver nuevo
- no changes en executor
- no cambio de comportamiento del runner
- no recipes touched
- no clicks reales

## Validaciones

- build: OK
- tests: OK
- secret scan del diff: OK
- `artifacts/` ignorado/no trackeado

## Riesgo conocido

`FailureKind` todavía no está integrado en evidence legacy del `RecipeRunner` ni del harness histórico.

Eso fue intencional para evitar cambios laterales de comportamiento en ejecución, exit codes o formatos ya usados.

Si esa integración se vuelve necesaria, debe entrar en un hito posterior explícito.

## Próximo paso recomendado

HITO-134 — Generalizable Target Resolver

Solo después de cerrar y publicar esta rama.
