# Agent Operations Namespace / Naming Audit M389

## Scope

M389 audits naming and namespace placement for the Agent Operations layer built between M344 and M388.

This is an architecture/naming audit only. It does not move namespaces, rename public contracts, delete legacy symbols, or change runtime behavior.

## Search

`rg` was unavailable in the shell, so discovery used PowerShell `Select-String` over source, tests, docs, and artifacts while excluding compiled `bin` and `obj` outputs.

Search terms included:

- `Nexa`
- `OneBrain`
- `NodalOs`
- `AgentOperations`
- `BrowserExecutor.Contracts`
- `BrowserExecutor.Cdp`
- `NexaMission`
- `NexaAgentTask`
- `NexaRunReport`
- `NexaFailureKind`
- `NexaEvidenceRef`
- `NodalOsAgent`
- `NodalOsRecipe`
- `NodalOsStep`
- `NodalOsVerification`
- `NodalOsEvidence`
- `NodalOsRedaction`

## Current Agent Operations Location

Agent Operations contracts currently live in:

- `src/OneBrain.BrowserExecutor.Contracts`

Agent Operations services currently live in:

- `src/OneBrain.BrowserExecutor.Cdp`

This is acceptable as a temporary compatibility location because no UI, orchestration API, package registry, persistence, or multi-runtime worker layer exists yet. It is not acceptable as a long-term architecture boundary.

## Existing Nexa Compatibility Symbols

The most relevant `Nexa*` Agent Operations symbols are:

- `NexaMission`
- `NexaMissionStatus`
- `NexaAgentTask`
- `NexaAgentTaskStatus`
- `NexaProgressNote`
- `NexaBlockerReport`
- `NexaBlockerKind`
- `NexaBlockerSeverity`
- `NexaBlockerResolutionMode`
- `NexaVerificationCheck`
- `NexaVerificationStatus`
- `NexaEvidenceRef`
- `NexaTaskValidationResult`
- `NexaFailureKind`
- `NexaFailureSeverity`
- `NexaTroubleshootingRecommendation`
- `NexaRunReport`
- `NexaRunStepReport`
- `NexaRunStatus`
- `NexaRunStepStatus`
- `NexaPolicyDecisionReport`
- `NexaApprovalReport`
- `NexaFailureReport`
- `NexaRunReportValidationResult`

These symbols are compatibility debt. They should not be used as the naming pattern for new Agent Operations types.

## Existing NodalOs Forward Symbols

Recent Agent Operations and cleanup work already uses `NodalOs*` for new contracts and services, including:

- `NodalOsAgentProgressReport`
- `NodalOsHumanDecisionRequest`
- `NodalOsVerificationSummary`
- `NodalOsVerificationBeforeDoneResult`
- `NodalOsRecipeManifest`
- `NodalOsRecipeStepManifest`
- `NodalOsRecipePolicyManifest`
- `NodalOsStepDefinition`
- `NodalOsStepValidationResult`
- `NodalOsRedactionResult`
- `NodalOsEvidenceBridgeRef`

This is the correct forward naming pattern.

## OneBrain Historical Namespace

The solution still uses `OneBrain.*` namespaces broadly. For Agent Operations this means:

- `OneBrain.BrowserExecutor.Contracts`
- `OneBrain.BrowserExecutor.Cdp`

`OneBrain.*` is a historical namespace, not the current product name. The current product name is NODAL OS.

## Risk Of Renaming Now

Renaming now would create avoidable risk:

- broad API churn across contracts, services, tests, docs, and artifacts;
- possible breakage of compatibility shims created in M377-M388;
- loss of traceability for historical reports;
- unnecessary conflict before package/skill/orchestration boundaries are settled.

## Risk Of Never Renaming

Never extracting Agent Operations would create long-term risk:

- Agent Operations would appear browser/CDP-bound even when used by desktop, package, skill, or orchestration layers;
- UI and orchestration consumers could depend directly on BrowserExecutor internals;
- future package/skill registry work would inherit browser-specific coupling;
- `Nexa*` could reappear as a current product naming convention.

## Recommendation

- Do not move namespaces or broadly rename now.
- Require new Agent Operations types to use `NodalOs*`.
- Treat existing `Nexa*` symbols as compatibility symbols.
- Treat `OneBrain.*` namespaces as historical implementation namespaces.
- Define future extraction boundary before UI/orchestration/package registry work.

## Forward Naming Rule

New Agent Operations public contracts and services must use `NodalOs*`.

Do not introduce new `Nexa*` Agent Operations types except for explicit compatibility shims.

## Compatibility Names

Existing `Nexa*` symbols remain valid until an extraction phase provides adapters/shims and tests.
