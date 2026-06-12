# HITO-100+101+102 - Process Memory + Workflow Retrieval + App Profile Manager

## Scope

This block adds the first operational process-memory layer for ONE BRAIN:

- HITO-100: Process Memory v0.
- HITO-101: deterministic Workflow Retrieval.
- HITO-102: App Profile Manager v0.

The implementation is local, safe, and non-executing. It does not add playback, browser automation, OpenAI calls, embeddings, or live web access.

## HITO-100 - Process Memory v0

Process memory entries are stored as runtime JSON under:

- `artifacts/process-memory/`

The store is ignored by Git through the existing `artifacts/` policy.

Core types:

- `ProcessMemoryEntry`
- `ProcessMemoryLink`
- `ProcessMemorySummary`
- `ProcessMemoryDecision`
- `ProcessMemoryError`
- `ProcessMemoryEvidenceLink`
- `ProcessMemoryStore`

Each entry can link to:

- `recordingSessionId`
- `timelineId`
- `candidateFlowId`
- `recipeDraftId`
- `recipeId`
- `approvalRequestId`
- `approvalDecisionId`
- `runId`
- `aiAuditId`
- `confidenceId`
- relative artifact paths

Safety rules:

- No secrets.
- No API keys.
- No full sensitive prompts by default.
- Secret-like content fails closed.
- Local artifact paths are normalized to relative/safe paths.
- Runtime artifacts stay under ignored `artifacts/`.

## HITO-101 - Workflow Retrieval

Retrieval is deterministic and local.

Core types:

- `WorkflowRetrievalQuery`
- `WorkflowRetrievalResult`
- `WorkflowRetrievalMatch`
- `WorkflowRetrievalService`

Search fields:

- text
- tags
- app/site
- domain
- risk level
- status
- minimum confidence score

Scoring:

- title match
- description/summary match
- step summary match
- tag match
- app/site match
- domain match
- status boost for `stable` / `supervised`
- status penalty for `rejected` / `archived`
- confidence boost

The result includes:

- process memory id
- score
- reasons
- linked recipe/candidate/timeline ids
- `safeToSuggest`
- `requiresHumanReview`

Retrieval never executes anything. It only suggests candidate process memories.

## HITO-102 - App Profile Manager v0

App profiles are runtime JSON artifacts under:

- `artifacts/app-profiles/`

Core types:

- `AppProfile`
- `AppProfileRiskPolicy`
- `AppProfileSelectorAlias`
- `AppProfileVersion`
- `AppProfileStore`
- `AppProfilePolicy`

Initial fixture profiles:

- demo product evidence fixture
- Mercado Libre readonly diagnostic
- Suministros Roca readonly
- Sodimac readonly
- ONE BRAIN Pilot local

Profile policy:

- Read-only by default.
- `external_fragile` requires `diagnostic_allowed`.
- Login is blocked by default.
- Cookie acceptance is blocked by default.
- Payment is blocked by default.
- Purchase is blocked by default.
- Safe click is not enabled by default.
- Interactive capabilities require explicit approval policy before activation.

## Pilot UI

New routes:

- `/memory`
- `/memory/search`
- `/memory/{id}`
- `/app-profiles`
- `/app-profiles/{id}`

The UI uses runtime artifacts when available and safe fixtures otherwise.

No route executes a recipe, launches playback, opens a browser, or calls OpenAI.

## Tests

Added coverage:

- process memory write/read
- process memory secret fail-closed
- process memory artifact path normalization
- retrieval by text/tag/app/domain
- retrieval score explanations
- archived process is not safe to suggest
- app profile write/read
- external fragile profile requires diagnostic allowed
- login/payment/purchase blocked by profile
- profile changes do not enable unsafe actions by default
- Pilot home links
- Pilot memory pages
- Pilot app profile pages

## Safety

Confirmed scope:

- No Firefox flaky regression.
- No OpenAI real calls.
- No API keys.
- No embeddings.
- No browser/web/red from the new feature.
- No playback.
- No clicks.
- No cookies accepted.
- No login.
- No cart.
- No purchase.
- No payment.

## Validation

Canonical validation uses the portable SDK:

```powershell
$ErrorActionPreference = "Stop"

$root = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo"
$dotnet = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe"

Set-Location $root

& $dotnet build OneBrain.slnx
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

& $dotnet test
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/exit-code-negative.json
$negativeExit = $LASTEXITCODE
Write-Host "NEGATIVE_EXIT_CODE=$negativeExit"
if ($negativeExit -eq 0) { throw "Failed recipe returned exit code 0" }

powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1 -Root $root -Dotnet $dotnet
if ($LASTEXITCODE -ne 0) { throw "Demo script failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe dry-run tools/recipes/demo-product-evidence-report.json
if ($LASTEXITCODE -ne 0) { throw "Demo markdown dry-run failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-report.json
if ($LASTEXITCODE -ne 0) { throw "Demo markdown run failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe dry-run tools/recipes/demo-product-evidence-html-report.json
if ($LASTEXITCODE -ne 0) { throw "Demo HTML dry-run failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-html-report.json
if ($LASTEXITCODE -ne 0) { throw "Demo HTML run failed" }

& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/mercadolibre-product-readonly.json
if ($LASTEXITCODE -ne 0) { throw "ML readonly/diagnostic failed" }

git status --ignored --short artifacts
git status --short
```

## Updated Percentage

- Core tecnico: 92-94%
- Safety: 91-93%
- Cleanup: 89-91%
- Validacion/CI confiable: 90-92%
- Web read-only: 87-89%
- Retail public read-only: 83-85%
- Evidence/reporting: 87-90%
- Demo/reproducibilidad: 92-94%
- Pilot UX/local ops: 68-72%
- Process memory/retrieval: 35-42%
- App profile governance: 35-42%
- MVP comercial serio: 73-76%
- Producto completo/pro: 52-56%
