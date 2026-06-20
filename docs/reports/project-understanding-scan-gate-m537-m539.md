# NODAL OS - Project Understanding Scan Gate M537-M539

## Resumen ejecutivo

M537-M539 agrega una capa previa al futuro scan de Project Understanding: precondiciones de path jail, contrato de consentimiento/scope preview y audit gate. El bloque no activa scan, filesystem, indexing, vectorization, LLM, cloud ni runtime.

Decision:

- `M537+M538+M539 CERRADO / PROJECT_UNDERSTANDING_SCAN_GATE_READY`

## Estado git inicial

- `git status --short`: limpio.
- Branch: `chrome-lab-001-extension-local-ai-bridge`.
- HEAD inicial: `116c63c899310deb5eed0d4558feb75de3bf2309`.
- Origin inicial: `116c63c899310deb5eed0d4558feb75de3bf2309`.
- Remote: `https://github.com/onediegopr/NodalOS.git`.
- Worktree: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`.
- Path prohibido no usado.

## Objetivo

- M537: Path Jail Implementation Preconditions.
- M538: Consent UI and Scope Preview Contract.
- M539: Real Scan Audit Gate.

## M537 - Path Jail Implementation Preconditions

Se agrego modelo y servicio de precondiciones para path jail futuro:

- Root path como ref simbolico.
- Politicas requeridas: canonicalization, containment, symlink, case sensitivity, drive boundary, network share, hidden file, exclusions, max depth, max files, max bytes.
- Requisitos de no mutation, cancellation, evidence plan, timeline plan y audit before activation.
- Readiness con `ReadyForRealPathJail=false`, `ReadyForFilesystemScan=false`, `ReadyForFileRead=false`, `ReadyForFileHashing=false`, `ReadyForDirectoryListing=false`.
- Forbidden capability flags en false.

## M538 - Consent UI and Scope Preview Contract

Se agrego contrato para draft de consentimiento y preview de scope:

- Consent request draft con `IsDraftOnly=true`, `IsNoOp=true`, `CanApproveRealScan=false`.
- Scope preview estimated-only con `UsesRealFilesystem=false`.
- Consent options no-op: approve/reject draft, narrower scope, explanation, needs review, export preview report.
- Consent result no autoriza scan, content handling, indexing, vectorization ni LLM context.

## M539 - Real Scan Audit Gate

Se agrego audit gate para bloquear cualquier scan futuro:

- Gate model con refs a path jail preconditions, consent scope preview, redaction policy, secret detection policy, evidence plan y timeline plan.
- Decision con readiness false para scan, folder enumeration, content handling, content fingerprinting, indexing, vectorization, LLM context y cloud sync.
- Audit dimensions para path jail, consent, scope preview, redaction, secret detection, exclusions, symlink, cancellation, no mutation, evidence, timeline, cloud disabled, LLM context disabled y audit before activation.
- Failure behavior ante marcadores de implementacion operativa.

## Confirmacion no-scan/no-file-read/no-index/no-LLM/no-runtime

- No real path jail.
- No real scan.
- No folder enumeration.
- No content read/write/delete.
- No content fingerprinting.
- No source-control commands.
- No vectorization creation.
- No index creation.
- No LLM context construction.
- No prompt creation.
- No provider activity.
- No network/HTTP.
- No cloud.
- No runtime execution.
- No productive persistence.

## Archivos creados/modificados

Contratos:

- `src/OneBrain.AgentOperations.Contracts/NodalOsPathJailPreconditionsContracts.cs`.
- `src/OneBrain.AgentOperations.Contracts/NodalOsConsentScopePreviewContracts.cs`.
- `src/OneBrain.AgentOperations.Contracts/NodalOsRealScanAuditGateContracts.cs`.

Servicios:

- `src/OneBrain.AgentOperations.Core/NodalOsPathJailPreconditionsServices.cs`.
- `src/OneBrain.AgentOperations.Core/NodalOsConsentScopePreviewServices.cs`.
- `src/OneBrain.AgentOperations.Core/NodalOsRealScanAuditGateServices.cs`.

Tests:

- `tests/OneBrain.Safety.Tests/NodalOsProjectUnderstandingScanGateM537M539Tests.cs`.

Artifacts:

- `artifacts/agent-operations/m539/path-jail-preconditions-summary.json`.
- `artifacts/agent-operations/m539/consent-scope-preview-summary.json`.
- `artifacts/agent-operations/m539/real-scan-audit-gate.json`.
- `artifacts/agent-operations/m539/project-understanding-scan-gate-preview.html`.

Roadmap:

- `docs/roadmap/nodal-os-roadmap-vnext.md`.
- `docs/roadmap/nodal-os-unified-roadmap-post-pause.md`.

## Tests nuevos

`NodalOsProjectUnderstandingScanGateM537M539Tests` cubre:

- Readiness false de path jail.
- Forbidden capability flags false.
- Politicas requeridas.
- Consent draft no-op/no-authority.
- Scope preview sin filesystem real.
- Consent result no autoriza capacidades futuras.
- Audit gate bloqueado.
- Audit dimensions.
- Required-before lists.
- Static HTML.
- Boundary checks para APIs y runtime primitives.

## Validaciones ejecutadas

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx --no-restore`: passed.
- Filtered tests M537-M539: 14 passed / 0 skipped / 0 failed.
- Full suite final rerun: 4104 passed / 37 skipped / 0 failed.
- Guard checks: passed.
- Frontend/Tauri/Rust: no aplica; no se detectaron `package.json` ni `Cargo.toml` aplicables con busqueda acotada.

## Guardrails confirmados

- Contract-first.
- Preconditions-only.
- Consent-preview-only.
- Audit-gate-only.
- Mock-safe.
- Ref-only.
- Redacted outputs.
- Deterministic.
- Local-first.
- Cloud disabled by default.
- LLM disabled by default.
- No filesystem access.

## Que NO se implemento

- Real path jail.
- Real scan.
- Folder enumeration.
- File handling.
- Content fingerprinting.
- Source-control commands.
- Vectorization.
- Indexing.
- LLM context build.
- Prompt creation.
- Provider activity.
- Runtime.
- Execution wiring.
- Planner runtime.
- Productive persistence.
- Cloud.

## Flaky

Si. La primera full suite observo el flaky heredado `BrowserRuntimeSmokeRunnerExecutesAllGatesOnFixture`, Gate 9 WebSocket aborted. Se hizo rerun sin cambios y paso limpio.

## Riesgos/pendientes

- Path jail real sigue pendiente.
- Consent UI productiva sigue pendiente.
- Secret detection policy e implementacion siguen pendientes.
- Cualquier scan futuro requiere nuevo hito explicito y audit.

## Porcentajes actualizados

- NODAL OS global: 98.8%.
- Agent Operations / Automation Layer: 98.0%.
- Core Runtime: 76%.
- Evidence/Timeline foundation: 89%.
- Approval foundation: 83%.
- Redaction/Safety foundation: 94%.
- Productization foundation: 73%.
- Mission Control UX: 74%.
- Workspace Local: 71%.
- Project Understanding foundation: 63%.
- LLM/Assignment: 74%.
- Cloud optional: 10%.

## Proximo bloque recomendado

- `M540+M541+M542 - Secret Detection Policy Preview + Exclusion Policy Pack + Scan Dry Run Contract`.

## Decision final

`M537+M538+M539 CERRADO / PROJECT_UNDERSTANDING_SCAN_GATE_READY`
