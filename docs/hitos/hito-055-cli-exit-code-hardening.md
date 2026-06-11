# HITO-055 - CLI Exit Code Hardening + Firefox Fixture Quarantine

## Bug

`recipe run` could print a JSON result with `Success=false` while the CLI process still returned exit code `0`.

Impact:

- PowerShell validation scripts continued after a failed recipe.
- A failed Firefox fixture regression passed through commit/push automation.
- `$LASTEXITCODE` was not a reliable gate for semantic recipe failures.

## Fix

- Added `CliExitCodes.FromRecipeResult`.
- `recipe run` now sets `Environment.ExitCode=1` when `RecipeRunResult.Success=false`.
- Missing recipe file, empty recipe and invalid JSON also set non-zero exit code.
- Successful `recipe run` keeps exit code `0`.
- `recipe dry-run` remains non-executing and returns `0` when the recipe loads and dry-run completes.

## Negative Control

Added `tools/recipes/exit-code-negative.json`.

It contains only `assert.equals` with `A` vs `B`.

Expected validation:

```powershell
& $dotnet run --project src/OneBrain.Cli -- recipe run tools/recipes/exit-code-negative.json
$negativeExit = $LASTEXITCODE
if ($negativeExit -eq 0) { throw "Failed recipe returned exit code 0" }
```

Expected result:

- JSON `Success=false`
- `$LASTEXITCODE != 0`
- No apps opened
- No browsers opened

## Firefox Fixture Decision

`tools/recipes/firefox-web-fixture-safe-click-report.json` remains useful as a diagnostic, but it is not reliable enough for mandatory regression.

Observed failure:

- Firefox opens, but the owned HWND can show `Nueva pestana` instead of the local fixture.
- `browser.read` does not see `ONE BRAIN Web Safe Click Fixture`.
- `safe.click` cannot find `Abrir informacion`.
- `browser.close` still closes the owned session and no Firefox orphan remains.

Decision:

- Marked Firefox fixture recipes as `diagnostic`, `flaky`, `autoRegression=false`.
- Do not use them as mandatory regression until `browser.open` verifies actual navigation for Firefox or launches an isolated owned profile safely.

## Validation Scope

Required:

- Build: 0 errors, 0 warnings.
- Tests pass.
- Negative recipe returns non-zero exit code.
- Positive recipes return exit code 0.
- Retail category read-only remains PASS.
- Mercado Libre remains read-only/preflight with no clicks.
- Controlled safe click remains PASS.
- Browser session stability remains PASS.

## Safety

This hito does not advance retail actions, banking, CRM, Google Ads or commercial execution.

No login, cart, purchase, payment, cookie acceptance, popup closing or commercial clicks are introduced.
