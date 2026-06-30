# Phase E Approval Human Review Audit Checklist

Use this checklist for formal closeout or external audit of Phase E.

## Scope

Audit these read-only source files:

- `src/OneBrain.Core/Approval/ApprovalHumanReviewReadOnlyFoundation.cs`
- `src/OneBrain.Core/Approval/ApprovalRiskDecisionReadOnlyGuard.cs`
- `src/OneBrain.Core/Approval/HumanReviewEvidenceContextLinkReadOnlyGuard.cs`
- `src/OneBrain.Core/Approval/ApprovalPacketReadOnlySurface.cs`
- `src/OneBrain.Core/Approval/HumanReviewPacketExportReadOnlyPreview.cs`

Audit these tests:

- `tests/OneBrain.Recipes.Tests/ApprovalHumanReviewReadOnlyFoundationTests.cs`
- `tests/OneBrain.Safety.Tests/ApprovalHumanReviewReadOnlyFoundationSafetyTests.cs`

Audit these docs:

- Phase E ADRs in `docs/adr/`
- Phase E QA reports in `docs/qa/`
- Phase E handoffs in `docs/handoff/`
- `docs/audit/phase-e-approval-human-review-artifact-index.md`

## Invariants

- Approval preview is not approval execution.
- Human review packet is not state mutation.
- Decision label is not command.
- Export preview is not physical export.
- Evidence link is not durable evidence.
- Context link is not trusted context by default.
- Existing writer/policy paths are not read-only Phase E paths.
- Release/commercial readiness remains NO-GO.
- Runtime/live readiness remains 0%.

## Forbidden References From Read-Only Paths

Confirm Phase E read-only files do not reference:

- `ApprovalArtifactWriter`
- `ApprovalPolicy`
- `ApprovalBindingValidator`
- `Pilot`
- `AgentOperations`
- `WriteArtifact`
- `WriteApproval`
- `ServiceCollection`
- `AddSingleton`
- `AddScoped`
- `AddTransient`

## Forbidden Capability Activation

Confirm no active:

- approval execution;
- approval state mutation;
- product action command;
- action button;
- physical export;
- clipboard;
- browser download;
- filesystem product read/write;
- workspace scan real;
- runtime/live/browser/CDP;
- WCU/OCR live;
- recipe execution;
- DB/dependency;
- migration runner/execution;
- provider/cloud/network;
- semantic/vector backend;
- LLM live;
- durable memory;
- protected post-M1345 browser execution.

## Expected Test Commands

- `dotnet build OneBrain.slnx`
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PhaseEApprovalHumanReview"`
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "TestCategory=PhaseEApprovalHumanReview"`
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=PhaseDContextWorkspaceMemory"`
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "TestCategory=PhaseDContextWorkspaceMemory"`
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=EvidenceIntelligence|TestCategory=EvidenceSafety|FullyQualifiedName~Evidence"`
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build --filter "TestCategory=EvidenceIntelligence|FullyQualifiedName~EvidenceIntelligence"`
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=RecipeManifest|TestCategory=RecipeRiskClassifier|TestCategory=RecipeStepRuntimePermission|TestCategory=Recipe"`
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-build`
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build`
- `npm test` in `stealth-engine`
- `npm run test:audit-safe` in `stealth-engine`
- Cloak/CDP equivalent filter:
  `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-build --filter "TestCategory=NoExtensionDefaultHarness|FullyQualifiedName~CdpMinimalNoExtensionProductSurface|TestCategory=ExtensionDeprecationHardening|TestCategory=ForkUpdateReleasePipeline"`
- `git diff --check`
- `git diff --cached --check`

## Known Retry/Flake Notes

- Full `OneBrain.Safety.Tests` has previously needed retry when clean-closure checks see new unstaged `.cs` files or BrowserRuntime smoke flakes.
- Do not report PASS unless final command output is PASS.

## Open Debt

- executable approval semantics;
- approval state mutation and durable audit trail;
- writer/policy path design;
- visible approval UI/action control design;
- physical export policy;
- provider/cloud and LLM live policy;
- semantic/vector policy;
- release/commercial readiness audit.
