# M9 Structured Evidence / Validation Prerequisite Hardening QA

Decision target: `NODAL_OS_M9_STRUCTURED_EVIDENCE_VALIDATION_PREREQUISITE_HARDENING`

Status: `GO_M9_STRUCTURED_EVIDENCE_VALIDATION_PREREQUISITE_HARDENING_READY`.

## Guard

- Repo: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `52bfb825ffa8d439acbcca27b08edba6a7a34c35`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Origin divergence at start: `0/0`
- M8 commit in ancestry: yes
- Working tree at start: clean

## Audit Summary

Existing inferred logic was identified in:

- `ReliableRecipeQualityPreflightContracts.cs`
- `ReliableRecipeEvalHarnessFixtureContracts.cs`
- `ReliableRecipeSandboxReadinessReports.cs`
- `ReliableRecipeDryRunAdapterReadiness.cs`
- `ReliableRecipeLabViewModels.cs`

M2 evidence and validation scoring maps legacy strings such as `evidence.download`, `evidence.validation`, `validation.timeline` and block kinds. M8 previously treated evidence/validation gates as satisfied when M2 missing requirement lists were empty. M9 keeps those legacy paths for compatibility but adds structured profiles and downgrades mapped or inferred requirements.

## Scope

Implemented:

- Structured evidence requirement contracts.
- Structured validation requirement contracts.
- Requirement source taxonomy.
- Missing behavior and adapter gate impact taxonomy.
- Structured prerequisite profile and completeness report.
- Deterministic structured prerequisite evaluator.
- Scenario catalog with 12 fixture-only cases.
- M2 quality/preflight integration.
- M5 eval report integration.
- M6 sandbox readiness integration.
- M8 adapter gate integration.
- M3 Recipe Lab read-only panel integration.
- Focused tests for 33 required cases.

Not implemented:

- No executable adapter.
- No runtime command.
- No browser launch.
- No CDP connection.
- No Playwright/Selenium/Puppeteer path.
- No Cloak mutation.
- No desktop/UIA/Win32 live behavior.
- No OCR live activation.
- No screenshot capture.
- No recorder runtime.
- No sandbox/VM/container.
- No provider/LLM call.
- No network call.
- No shell/process runner.
- No productive filesystem side effect.

## Protected Scope Statement

OCR files touched: no.
OCR behavior changed: no.
OCR gates changed: no.
OCR live activation changed: no.
OCR referenced only as supporting evidence/perception requirement: yes.

Perception runtime added: no.
Live DOM/browser capture added: no.
Live accessibility/UIA/Win32 capture added: no.
Provider/VLM/LLM call added: no.

Recorder runtime added: no.
Mouse/keyboard/clipboard/screen capture added: no.
Background listener added: no.

Sandbox runtime added: no.
VM/container/Docker added: no.
Browser/desktop sandbox launch added: no.

Runtime/adapter implementation added: no.
Design/readiness only: yes.

## Structured Prerequisite Statement

Explicit requirements added: yes.
Fixture explicit requirements added: yes.
Legacy mapped requirements downgraded to warning: yes.
Inferred requirements identified: yes.
Missing critical requirements block adapter gate: yes.
M8 adapter gates use structured prerequisites: yes.

## Test Coverage

Focused category:

- `StructuredEvidenceValidationPrerequisites`

Coverage includes:

- scenario catalog and stable IDs,
- no live URLs or secrets,
- explicit invoice prerequisites,
- inferred requirements warning,
- missing download artifact evidence blocking,
- missing submit post-validation blocking,
- external side-effect approval/evidence blocking,
- OCR-only sensitive perception validation blocking,
- recorder draft human review evidence blocking,
- CAPTCHA/2FA human intervention evidence blocking,
- sandbox readiness assertion blocking,
- eval expected outcome assertion blocking,
- high-risk policy block even with explicit requirements,
- legacy mapped requirements pass with warning,
- explicit and inferred ratios,
- M2/M5/M6/M8/M3 integration,
- no OCR/perception/recorder/sandbox/adapter runtime exposure.

## Validation Results

- `dotnet restore .\OneBrain.slnx` - PASS.
- `dotnet build .\OneBrain.slnx --no-restore` - PASS, 0 warnings, 0 errors.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter TestCategory=StructuredEvidenceValidationPrerequisites` - PASS, 33/33.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter TestCategory=ProtectedDryRunAdapterReadinessDesignAudit` - PASS, 32/32.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter TestCategory=ReliableRecipeEvalHarnessFixtureScenarios` - PASS, 32/32.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter TestCategory=ComputerUseSandboxReadinessReports` - PASS, 30/30.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build --filter TestCategory=ReliableRecipeLabReadOnlySurface` - PASS, 18/18.
- `dotnet test .\tests\OneBrain.Recipes.Tests\OneBrain.Recipes.Tests.csproj --no-build` - PASS, 1158/1158.
- `dotnet test .\tests\OneBrain.Safety.Tests\OneBrain.Safety.Tests.csproj --no-build --filter FullyQualifiedName~Recipe` - PASS, 155 passed, 1 skipped.
- `git diff --check` - PASS.
- `git diff --cached --check` - PASS.
- Protected scope scan - PASS.
- OCR protected scope scan - PASS.
- Perception no-live scan - PASS.
- Recorder no-live scan - PASS.
- Sandbox no-runtime scan - PASS.
- Runtime adapter no-implementation scan - PASS.
- Secret scan - PASS.
- No-live/no-action scan - PASS; hits are blocked capabilities, negated copy or negative assertions.
- Dependency scan - PASS.
- UI build/lint - not applicable; no UI files changed.
- JSON validation - not applicable; no JSON files changed.

## Product Copy Boundary

Allowed wording:

- Structured evidence required.
- Structured validation required.
- Inferred requirement.
- Explicit requirement.
- Missing proof.
- Adapter gate blocked.
- Runtime not enabled.

Forbidden wording remains blocked:

- Ready to execute.
- Validated live.
- Evidence captured live.
- Proof complete for runtime.
- Run now.
- Autonomous.
- Adapter enabled.

## Percentage Update

Before M9:

- Overall new upgrade: 88%
- Reliable Recipe contracts: 90%
- Validation/policy gates: 84%
- Evidence/timeline recipe linkage: 78%
- Recorder draft readiness: 80%
- Eval harness readiness: 84%
- Sandbox readiness: 88%
- Perception stack formalization: 92%
- Product surface readiness for Recipe Lab: 86%
- Dry-run adapter readiness design: 65%
- Runtime real autonomy: 0%

After M9 target:

- Overall new upgrade: 92%
- Reliable Recipe contracts: 94%
- Validation/policy gates: 92%
- Evidence/timeline recipe linkage: 92%
- Recorder draft readiness: 84%
- Eval harness readiness: 88%
- Sandbox readiness: 92%
- Perception stack formalization: 94%
- Product surface readiness for Recipe Lab: 90%
- Dry-run adapter readiness design: 78%
- Runtime real autonomy: 0%

## Recommendation

Next block should add structured prerequisite authoring/review fixtures and migration reports from legacy evidence/validation refs to explicit structured requirements. It should remain no-runtime unless a separate protected audit authorizes runtime-adjacent work.
