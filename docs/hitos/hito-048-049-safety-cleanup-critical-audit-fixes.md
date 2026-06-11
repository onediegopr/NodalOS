# HITO-048+049 - Safety/Cleanup Critical Audit Fixes

## Status

Implemented as surgical fixes after the global audit. No retail, banking, CRM, Google Ads, login, cart, purchase, payment, or commercial-click capability was added.

## Audit Findings Addressed

| Finding | Fix |
| --- | --- |
| Spanish safety/preflight mojibake | `ClickPreflightEvaluator` now uses real UTF-8 Spanish terms such as `crédito`, `débito`, `iniciar sesión`, `contraseña`, `financiación`. `MinimalSafetyGuard` also uses `cerrar pestaña` with real UTF-8. |
| `app.close` counted failed closes as closed | `app.close` now increments `closedCount` only when `BrowserSession.Close()` returns `Success=true`. |
| Generic Explorer close could close user windows | `app.close explorer` is blocked/no-op until owned app session tracking exists. |
| `safe.click` was not classified as sensitive | CLI dry-run and RecipeRunner approval gating now classify `safe.click` as sensitive. |
| `requiresReview` could become executable | `ApprovalManifestBuilder` now keeps `requiresReview` non-executable in every mode. |
| `safe.click` trusted forged approval variables | `approval.manifest` exposes target/mode/decision/risk/policy/hash fields; `safe.click` validates them before input. |
| BrowserSession unsupported message omitted Firefox | Error message now says `edge, chrome, firefox`. |

## Binding Policy v1

`approval.manifest` now records:

- `targetText`
- `mode`
- `decision`
- `riskCategory`
- `riskLevel`
- `policyVersion`
- `evidenceHash`
- `executionAllowedInThisHito`

`safe.click` blocks before execution if:

- approval target does not match click target;
- approval mode does not match click mode;
- policy version is not `approval-v2`;
- decision is `requiresReview` or not executable;
- evidence hash is missing or invalid;
- `executionAllowedInThisHito` is not true.

If `safe.click` does not explicitly declare `mode`, it uses the mode recorded by `approval.manifest`. This preserves existing valid controlled/non-commercial recipes while still enforcing binding.

## Explicitly Not Solved In This Hito

- Real `AppSessionTracker` for Calculator, Notepad, Explorer.
- Full cleanup to zero across all legacy/manual recipes.
- Splitting `RecipeRunner` into focused step handlers.
- `ProfilePolicyGuard` enforcement for profile safety metadata.
- Artifact redaction for screenshots/snapshots/logs.
- Windows CI with required .NET SDK.
- Strong signed approval tokens beyond deterministic `evidenceHash`.
- Commercial safe click.
- Login/cart/purchase/payment automation.

## Recipe Risk Documentation

Recipes that open Notepad, Explorer, Calculator, Edge, or Firefox without guaranteed owned cleanup should be treated as manual/legacy/diagnostic until a recipe taxonomy is introduced.

Known risky/manual candidates include:

- `calculator-to-notepad.json`
- `real-smoke-windows-pack.json`
- `web-real-task-pack.json`
- `web-example-profile-report.json`
- `recorded-notepad-sample.json`
- `notepad-report-smoke.json`
- `explorer-temp-smoke.json`
- `browser-smoke.json`
- `edge-example-smoke.json`

## Validation Checklist

Required before closing the hito:

- `dotnet build OneBrain.slnx`
- `dotnet test`
- dry-run confirms `safe.click` is sensitive
- controlled safe click still closes Calculator
- fixture safe click still closes browser
- Mercado Libre recipes remain read-only/preflight/extraction only
- no app/browser orphan remains after executed recipes
- no commercial clicks
- no login/cart/purchase/payment
