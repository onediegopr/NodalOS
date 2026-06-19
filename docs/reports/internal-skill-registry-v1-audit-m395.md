# NODAL OS Internal Skill Registry V1 Audit M395

## Scope

M395 audited whether NODAL OS already had an internal skill registry boundary before adding V1 contracts. The worktree was `C:\DESARROLLO\NodalOS\Codigo-m12-audit` on branch `chrome-lab-001-extension-local-ai-bridge` at base commit `4c37faa`.

`rg` was not available in this environment, so discovery used PowerShell `Select-String` with: `SkillRegistry`, `InternalSkillRegistry`, `Registry`, `PackageRegistry`, `PackageSkillManifest`, `NodalOsPackageManifest`, `NodalOsSkillManifest`, `PackageId`, `SkillId`, `Catalog`, `ApprovedForCatalog`, `InternalOnly`, `RuntimeExecutionAllowed`, and `RuntimeExecutionDeferred`.

## Existing Models

| Model | Role | Registry relevance |
| --- | --- | --- |
| `NodalOsPackageManifest` | Describes internal package metadata, provenance, skills, evidence requirements, and runtime-deferred flags. | Source input for package registry entries. |
| `NodalOsSkillManifest` | Describes governed skill metadata, capabilities, risk, approvals, domains, and evidence requirements. | Source input for skill registry entries. |
| `NodalOsPackageSkillManifestValidator` | Validates manifest-local catalog policy. | Registry should reuse its invariants but not replace it. |
| `NodalOsRecipeManifest` | Describes automation recipes. | Related concept only; recipes are not registry entries in V1. |
| `NodalOsStepDefinition` | Describes primitive step metadata. | Related concept only; steps are referenced by skills but not indexed as registry entries in V1. |
| Namespace ADR | Defines `NodalOs*` forward naming and temporary BrowserExecutor placement. | Registry types must follow it. |

No existing `NodalOsInternalSkillRegistry*` contracts or services were found.

## Manifest vs Registry

Package and Skill Manifests are source documents. The Internal Skill Registry V1 is an in-memory/catalog snapshot that indexes manifest metadata for lookup and governance review.

The registry can say:

- visible;
- draft;
- hidden;
- deprecated;
- blocked;
- valid or invalid catalog metadata.

The registry cannot say:

- executable;
- installed;
- worker-ready;
- approved for runtime;
- marketplace-ready.

## Reuse

- Package / Skill Manifest V1 is the source of registry entries.
- Common redaction remains the sensitive-content classifier.
- Runtime wording from M386-M394 remains mandatory: `RuntimeExecutionAllowed=false`, `RuntimeExecutionDeferred=true`, `RequiresGlobalPolicyEvaluation=true`.
- Agent Operations temporary placement in `BrowserExecutor.*` remains unchanged.

## Additions

- Registry entry model.
- Registry snapshot model.
- Registry validation result.
- Query model.
- Builder from package manifests.
- Query service.
- JSON serializer.
- Fixture-backed registry.

## Not Implemented

- No persistence DB.
- No package registry service backed by storage.
- No worker runtime.
- No package install/uninstall.
- No skill execution.
- No marketplace.
- No UI.
- No orchestration API.

## Runtime Confusion Risk

The risk is future code treating registry visibility as execution permission. V1 mitigates this by requiring every registry entry to preserve:

- `InternalOnly=true`;
- `RuntimeExecutionAllowed=false`;
- `RuntimeExecutionDeferred=true`;
- `RequiresGlobalPolicyEvaluation=true`.

Lookups return catalog metadata only, never commands or executable handles.

## Temporary Location

Contracts are placed in `src/OneBrain.BrowserExecutor.Contracts/NodalOsInternalSkillRegistryContracts.cs`.

Services are placed in `src/OneBrain.BrowserExecutor.Cdp/NodalOsInternalSkillRegistryServices.cs`.

This follows the current Agent Operations convention and remains a future extraction candidate.

## Naming

All new public symbols use `NodalOs*`. No new `Nexa*` types are introduced.
