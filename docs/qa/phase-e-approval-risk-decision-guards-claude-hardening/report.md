# QA Report: Phase E Approval Risk Decision Guards With Claude Hardening

Decision target: `GO_PHASE_E_APPROVAL_RISK_DECISION_GUARDS_WITH_CLAUDE_HARDENING_READY`

## Scope

This hito hardens Phase E Approval/Human Review risk and decision semantics while preserving read-only, fixture-safe, in-memory behavior.

Included:

- Claude P2/P3 disposition.
- Schema compatibility `Rejected` dead branch collapse.
- Phase D expected decision assertions for guard fixtures.
- Approval risk/decision read-only guard.
- Recipes and Safety tests.
- Source scans and no-side-effect proof.

Excluded:

- approval execution;
- approval state mutation;
- product action buttons;
- runtime/live;
- filesystem product read/write;
- DB/dependency;
- provider/cloud;
- semantic/vector backend;
- LLM live;
- durable memory;
- physical export.

## Claude Findings

- `P2-001`: mitigated by read-only source reference scans against `ApprovalArtifactWriter`, `ApprovalPolicy`, `ApprovalBindingValidator`, `Pilot`, `AgentOperations` and writer/policy execution terms.
- `P2-002`: documented as declarative proof; mitigated with scans and non-product approval artifact snapshot test.
- `P2-003`: fixed by removing unreachable `Rejected` from schema compatibility decision.
- `P2-004`: fixed by comparing expected fixture decisions in Phase D guard tests.
- `P3-001`: reinforced with exact expected issue checks for authority/freshness fixtures.
- `P3-002`: actual Cloak/CDP dotnet filter command is reported in validation output.
- `P3-003`: no large file refactor in this hito; documented debt.
- `P3-004`: helper naming improved to `BlockedOrExcluded`.

## Validation Matrix

Executed:

- `dotnet build OneBrain.slnx` - PASS.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter "TestCategory=PhaseEApprovalHumanReview"` - PASS, 10 tests.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter "TestCategory=PhaseEApprovalHumanReview"` - PASS, 13 tests.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter "TestCategory=PhaseDContextWorkspaceMemory"` - PASS, 36 tests.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter "TestCategory=PhaseDContextWorkspaceMemory"` - PASS, 37 tests.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter "TestCategory=EvidenceIntelligence|TestCategory=EvidenceSafety|FullyQualifiedName~Evidence"` - PASS, 759 tests.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter "TestCategory=EvidenceIntelligence|FullyQualifiedName~EvidenceIntelligence"` - PASS, 74 tests.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter "TestCategory=RecipeManifest|TestCategory=RecipeRiskClassifier|TestCategory=RecipeStepRuntimePermission|TestCategory=Recipe"` - PASS, 222 tests.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj` - PASS, 1414 tests.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj` - PASS on retry with expanded timeout, 5928 passed, 37 skipped.
- `npm test` in `stealth-engine` - PASS, 29 tests.
- `npm run test:audit-safe` in `stealth-engine` - PASS, 29 tests.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter "TestCategory=NoExtensionDefaultHarness|FullyQualifiedName~CdpMinimalNoExtensionProductSurface|TestCategory=ExtensionDeprecationHardening|TestCategory=ForkUpdateReleasePipeline"` - PASS, 30 tests.
- `git diff --check` - PASS.
- `git diff --cached --check` - PASS.
- Required staged scans - PASS after review; broad scans only hit detector strings inside tests, strict source scans found no real capability activation.

Retries:

- One early Safety filter run failed due to a parallel build lock on `OneBrain.Core.dll`; after `dotnet build-server shutdown`, the retry passed.
- Full `OneBrain.Safety.Tests` first hit tool timeout at 244 seconds; retry with a larger timeout passed.

## No-Side-Effect Proof

The guard and foundation:

- are deterministic;
- use fixture catalogs only;
- expose preview-only decision options;
- always set approval execution allowed to false;
- always set state mutation allowed to false;
- always set product action allowed to false;
- keep provider/cloud, semantic/vector and LLM live disabled;
- do not reference approval writer/policy/runtime execution paths from read-only files.

## Risks

No P0/P1 identified during implementation.

Remaining P2/P3 debt:

- approval writer/policy live paths remain preexisting and out of read-only scope;
- no-side-effect proof is declarative plus scan/test backed, not full runtime instrumentation;
- large context file refactor remains deferred;
- physical approval workflow design remains blocked.
