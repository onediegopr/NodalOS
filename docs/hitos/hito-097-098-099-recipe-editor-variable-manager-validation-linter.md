# HITO-097+098+099 - Recipe Editor + Variable Manager + Validation Linter

## Scope

This block adds the first safe editing layer for ONE BRAIN recipes and candidate flows.

- HITO-097: Recipe Editor v0.
- HITO-098: Variable Manager.
- HITO-099: Recipe Validation / Linter.

The editor is intentionally conservative: it creates draft artifacts and does not overwrite stable recipe JSON automatically.

## HITO-097 - Recipe Editor v0

Pilot now exposes:

- `/recipes`
- `/recipes/{id}`
- `/recipes/{id}/edit`

The editor lists allowlisted recipes and shows:

- recipe id
- title/name
- description
- risk level
- confidence status
- recipe path
- human-readable steps
- validation result
- variables

Safe editable fields:

- title/name
- description
- notes
- tags
- human-readable labels

Blocked free-form edits:

- step action kind
- arbitrary commands
- sensitive action args
- paths without validation
- browser/click/type/submit/login/cookie/payment/purchase actions

Saved edits are draft/candidate artifacts under:

- `artifacts/recipe-drafts/`

Stable recipe JSON is not overwritten by Pilot.

## HITO-098 - Variable Manager

Pilot now exposes:

- `/variables`
- `/recipes/{id}/variables`

The variable manager detects variables from recipe templates and explicit recipe variables.

Supported variable metadata:

- name
- type: `text`, `number`, `url`, `file_path`, `selector`, `product`, `contact`, `message`, `amount`, `date`, `boolean`
- required flag
- default value
- example value
- sensitivity: `public`, `internal`, `sensitive`, `secret`
- redaction flag
- validation regex
- allowed values
- min/max

Rules:

- Secret variables are not shown in full.
- Secret variables should not be stored as plain recipe values.
- Sensitive values are masked in Pilot.
- The manager does not execute recipes.

## HITO-099 - Recipe Validation / Linter

The linter produces:

- `RecipeValidationResult`
- `RecipeValidationIssue`
- severity: `info`, `warning`, `error`, `blocked`
- code
- message
- field/path
- remediation suggestion
- `canRun`
- `canPromote`

The linter detects:

- non-allowlisted actions
- sensitive actions without approval policy
- arbitrary command content
- dangerous absolute paths
- secret-like content
- missing required variables
- missing metadata
- browser/web external actions without diagnostic/read-only context
- payment/purchase/login/cookies

Critical actions are blocked by default:

- payment
- purchase
- login
- cookies
- submit/click/type without approval policy
- arbitrary commands/scripts

## Files

Created:

- `src/OneBrain.Core/Recipes/Editing/RecipeEditingModels.cs`
- `src/OneBrain.Core/Recipes/Editing/RecipeEditPolicy.cs`
- `src/OneBrain.Core/Recipes/Editing/RecipeDraftStore.cs`
- `src/OneBrain.Core/Recipes/Editing/RecipeEditorService.cs`
- `src/OneBrain.Core/Recipes/Editing/RecipeVariableManager.cs`
- `src/OneBrain.Core/Recipes/Editing/RecipeValidationModels.cs`
- `src/OneBrain.Core/Recipes/Editing/RecipeLinter.cs`
- `tests/OneBrain.Recipes.Tests/RecipeEditorTests.cs`
- `tests/OneBrain.Recipes.Tests/RecipeVariableManagerTests.cs`
- `tests/OneBrain.Recipes.Tests/RecipeLinterTests.cs`
- `tests/OneBrain.Recipes.Tests/PilotRecipeEditorTests.cs`

Modified:

- `src/OneBrain.Pilot/Program.cs`
- `src/OneBrain.Pilot/PilotHomePageRenderer.cs`

## Safety

Maintained:

- no browser/web introduced by editor/variables/linter
- no recipe execution from editor
- no stable recipe overwrite
- no arbitrary command execution
- no playback
- no OpenAI calls
- no API keys committed
- no login
- no cookies
- no cart
- no purchase
- no payment
- no Firefox flaky regression required

## Validation

Required:

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

- Core tecnico: 93-95%
- Safety: 92-94%
- Cleanup: 89-91%
- Validacion/CI confiable: 91-93%
- Web read-only: 87-89%
- Retail public read-only: 82-84%
- Evidence/reporting: 86-89%
- Demo/reproducibilidad: 91-93%
- Pilot UX: 68-72%
- AI architecture: 58-62%
- Auditability: 62-66%
- Recipe authoring: 45-50%
- MVP comercial serio: 74-77%
- Producto completo/pro: 52-55%
