# NODAL OS Package / Skill Manifest V1 Audit M392

## Scope

M392 audited the current Agent Operations contracts before adding Package / Skill Manifest V1. The worktree was `C:\DESARROLLO\NodalOS\Codigo-m12-audit` on branch `chrome-lab-001-extension-local-ai-bridge` at base commit `f14b64b`.

`rg` was not available in this environment, so discovery used PowerShell `Select-String` with these terms: `PackageManifest`, `SkillManifest`, `SkillRegistry`, `PackageRegistry`, `WorkerBoundary`, `Skill`, `Package`, `Registry`, `Connector`, `Tool`, `RecipeManifest`, `StepLibrary`, `Capability`, `Provenance`, `Version`, `RuntimeExecutionDeferred`, and `RequiresGlobalPolicyEvaluation`.

## Existing Concepts

| Area | Existing model | Relevance |
| --- | --- | --- |
| Recipe Manifest | `NodalOsRecipeManifest`, `NodalOsRecipeStepManifest`, `NodalOsRecipeManifestValidator` | Defines automation intent and local recipe policy. It is not a package or skill boundary. |
| Step Library | `NodalOsStepDefinition`, `NodalOsStepValidationResult`, `NodalOsStepLibraryValidator` | Defines catalog-level step metadata. It does not describe reusable skills or packages. |
| Runtime wording | Recipe and Step contracts now expose `RuntimeExecutionAllowed=false`, `RuntimeExecutionDeferred=true`, and `RequiresGlobalPolicyEvaluation=true` | Package / Skill Manifest must follow the same safety wording. |
| Redaction | `NodalOsRedactionService`, `NodalOsSensitiveContentClassifier` | Required for manifest secret detection. |
| Evidence bridge | `NodalOsEvidenceBridgeRef` and bridge service | Package/skill manifests should declare evidence requirements but not persist evidence. |
| Namespace ADR | `agent-operations-namespace-naming-adr.md` | New types must use `NodalOs*`; no namespace move in this block. |
| Robomotion roadmap note | `robomotion-package-skill-worker-roadmap-note.md` | Inspires package/skill/worker vocabulary, but no registry or worker runtime is implemented. |

No existing `NodalOsPackageManifest` or `NodalOsSkillManifest` contract was found.

## Reuse

- Reuse `OneBrain.BrowserExecutor.Contracts` and `OneBrain.BrowserExecutor.Cdp` temporarily, matching current Agent Operations placement.
- Reuse common redaction for sensitive marker detection.
- Reuse the runtime-permission wording established in M386-M388.
- Reuse Recipe/Step concepts only by reference metadata, not by execution.

## Additions

- `NodalOsPackageManifest`
- `NodalOsSkillManifest`
- status, capability, and risk enums
- serializer/validator
- fixture set for internal read-only, draft, blocked, deprecated, file-transfer, data-entry, and navigation cases

## Boundary Definitions

| Term | Meaning in V1 |
| --- | --- |
| Recipe | Describes an automation definition and local recipe policy. |
| Step | Describes a catalogued primitive step kind and governance metadata. |
| Skill | Describes a reusable governed capability with risk, approvals, domains, evidence requirements, and provenance context. |
| Package | Groups skills with publisher, provenance, version, tags, and internal catalog status. |
| Registry | Future index/discovery mechanism. Not implemented in M392-M394. |
| Worker | Future execution boundary. Not implemented in M392-M394. |

## Manifest vs Execution Risk

The main risk is future code reading `ApprovedForCatalog` as permission to execute. M392-M394 prevents that by requiring:

- `RuntimeExecutionAllowed=false`
- `RuntimeExecutionDeferred=true`
- `RequiresGlobalPolicyEvaluation=true`
- `InternalOnly=true`

Catalog policy is explicitly separate from runtime permission.

## Temporary Location Decision

Package / Skill Manifest V1 is placed in:

- `src/OneBrain.BrowserExecutor.Contracts/NodalOsPackageSkillManifestContracts.cs`
- `src/OneBrain.BrowserExecutor.Cdp/NodalOsPackageSkillManifestServices.cs`

This follows current Agent Operations convention. The namespace ADR still marks this as temporary and recommends future extraction into an Agent Operations boundary before registry, orchestration, or UI.

## Naming Decision

All new public types use the `NodalOs*` prefix. No new `Nexa*` type is introduced.

## Non-Goals

- No package registry.
- No skill registry.
- No package installer.
- No worker runtime.
- No marketplace.
- No UI.
- No orchestration API.
- No recipe or step execution.
- No namespace move.
- No broad rename.
