# HITO-094+095+096 - Run History + Execution UI + AI Usage Audit Logs

## Scope

This block adds local auditability for ONE BRAIN runs and AI routing decisions.

- HITO-094: Run History Store.
- HITO-095: Execution History UI in Pilot.
- HITO-096: AI Usage / Audit Logs.

It does not add playback, autonomous execution, real OpenAI calls, browser automation, cookies, login, cart, purchase, payment, or new scraping.

## HITO-094 - Run History Store

Added local JSON history under `artifacts/run-history/`.

Each run records:

- `runId`
- `startedAtUtc`
- `endedAtUtc`
- `status`: `pending`, `running`, `succeeded`, `failed`, `diagnostic`, `blocked`
- `source`: `pilot`, `cli`, `recipe`, `recording`, `approval`, `ai_router`, `mock`
- `recipeId`
- `candidateFlowId`
- `approvalRequestId`
- `approvalDecisionId`
- `recordingSessionId`
- `timelineId`
- `confidenceId`
- `aiRoutingDecisionId`
- `exitCode`
- safety counters for clicks, cookies, login, cart, purchase, and payment
- relative artifact paths
- sanitized error summary
- notes

Rules:

- No API keys.
- No secrets.
- Secret-like content fails closed.
- Runtime artifacts stay under ignored `artifacts/`.
- Absolute local paths are normalized to safe relative paths when possible.

## HITO-095 - Execution History UI

Pilot now exposes:

- `/runs`
- `/runs/{id}`

The UI shows:

- recent runs
- status
- timestamp
- source
- recipe or candidate flow
- safety summary
- artifact paths
- approval references
- confidence references
- AI routing decision references
- sanitized error summary

If no runtime history exists, Pilot shows safe local fixtures only.

The UI does not open reports automatically and does not execute anything.

## HITO-096 - AI Usage / Audit Logs

Added local AI audit logs under `artifacts/ai-audit/`.

Each AI audit record includes:

- `aiAuditId`
- timestamp
- recommended/used profile
- provider
- model/config name
- task type
- risk level
- vision requirement
- human approval requirement
- fallback state
- budget decision
- estimated cost
- actual cost, optional/null
- tokens in/out, optional/null
- result status
- sanitized reason/error

Pilot now exposes:

- `/ai/audit`

The audit UI explains:

- which profile was selected
- why routing happened
- whether fallback was used
- whether budget blocked execution
- whether the decision failed closed

This hito still performs no real provider calls. Any future OpenAI call must pass through `OneBrain.AI.ModelRouter`.

## Files

Created:

- `src/OneBrain.Core/History/RunHistoryModels.cs`
- `src/OneBrain.Core/History/RunHistoryStore.cs`
- `src/OneBrain.Core/History/AIAuditModels.cs`
- `src/OneBrain.Core/History/AIAuditLogStore.cs`
- `src/OneBrain.Core/History/HistorySanitizer.cs`
- `src/OneBrain.Core/History/HistoryDemoFixture.cs`
- `tests/OneBrain.Recipes.Tests/RunHistoryStoreTests.cs`
- `tests/OneBrain.Recipes.Tests/AIAuditLogStoreTests.cs`
- `tests/OneBrain.Recipes.Tests/PilotHistoryAuditTests.cs`

Modified:

- `src/OneBrain.Pilot/Program.cs`
- `src/OneBrain.Pilot/PilotHomePageRenderer.cs`

## Safety

Maintained:

- 0 clicks
- 0 cookies accepted
- 0 login
- 0 cart
- 0 purchase
- 0 payment
- no Firefox flaky regression required
- no real OpenAI call
- no API keys committed
- no secrets logged
- no full prompt storage by default

## Validation

Required validation:

```powershell
$ErrorActionPreference = "Stop"

$root = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Codigo"
$dotnet = "C:\Users\diego\OneDrive\PERSONAL\ONE Brain\Herramientas\dotnet-sdk-11.0.100-preview.5.26302.115-win-x64\dotnet.exe"

Set-Location $root

& $dotnet build OneBrain.slnx
& $dotnet test
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/exit-code-negative.json
powershell -ExecutionPolicy Bypass -File tools/scripts/run-demo-product-evidence.ps1 -Root $root -Dotnet $dotnet
& $dotnet run --project src/OneBrain.Cli -- recipe dry-run tools/recipes/demo-product-evidence-report.json
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-report.json
& $dotnet run --project src/OneBrain.Cli -- recipe dry-run tools/recipes/demo-product-evidence-html-report.json
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/demo-product-evidence-html-report.json
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/mercadolibre-product-readonly.json
git status --ignored --short artifacts
git status --short
```

## Current Percentage

- Core tecnico: 92-94%
- Safety: 91-93%
- Cleanup: 89-91%
- Validacion/CI confiable: 90-92%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 86-89%
- Demo/reproducibilidad: 91-93%
- Pilot UX: 62-66%
- AI architecture: 58-62%
- Auditability: 60-64%
- MVP comercial serio: 72-75%
- Producto completo/pro: 50-53%
