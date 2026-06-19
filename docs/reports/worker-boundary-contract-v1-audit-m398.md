# NODAL OS Worker Boundary Contract V1 Audit M398

## Scope

M398 audited existing worker, registry, package/skill, evidence, redaction, run report, and failure taxonomy concepts before adding Worker Boundary Contract V1. The worktree was `C:\DESARROLLO\NodalOS\Codigo-m12-audit` on branch `chrome-lab-001-extension-local-ai-bridge` at base commit `7a4de22`.

`rg` was not available, so discovery used PowerShell `Select-String` with: `WorkerBoundary`, `Worker`, `WorkerRuntime`, `SkillRegistry`, `InternalSkillRegistry`, `PackageSkillManifest`, `Capability`, `RuntimeExecutionAllowed`, `RuntimeExecutionDeferred`, `RequiresGlobalPolicyEvaluation`, `RunReport`, `FailureKind`, `EvidenceBridge`, `Redaction`, `Health`, and `Status`.

## Existing Concepts

| Area | Existing concept | Relevance |
| --- | --- | --- |
| Package / Skill Manifest | `NodalOsPackageManifest`, `NodalOsSkillManifest` | Source metadata for packages and skills a future worker may declare support for. |
| Internal Skill Registry | `NodalOsSkillRegistryEntry`, registry snapshot/query | Catalog metadata that a worker boundary can reference, without execution permission. |
| Evidence Bridge | `NodalOsEvidenceBridgeRef` | Worker response envelopes may carry evidence refs as no-authority evidence. |
| Failure Taxonomy | `NexaFailureKind` compatibility enum | Worker response envelopes may report typed failures. |
| Common Redaction | `NodalOsRedactionService` | Worker manifests, health reports, and envelopes must reject secret-like content. |
| Robomotion roadmap note | package/skill/worker future boundary | Inspires vocabulary only; no worker runtime is copied or implemented. |

No existing `NodalOsWorkerBoundary*` model was found.

## Boundary Terms

| Term | Meaning |
| --- | --- |
| Skill | Governed capability metadata from Package / Skill Manifest V1. |
| Package | Internal grouping of skill manifests and provenance. |
| Registry | Internal catalog snapshot over package/skill metadata. |
| Worker | Future capability host boundary declaration. |
| Worker Runtime | Future executable runtime/process. Not implemented here. |
| Orchestration API | Future API that may route work. Not implemented here. |

## Reuse

- Package/Skill IDs from manifest/registry.
- Evidence bridge refs for no-authority evidence.
- Failure taxonomy for typed failure reporting.
- Common redaction for sensitive-content checks.
- Runtime-deferred wording from M386-M397.

## Added

- Worker identity/status/health contracts.
- Declarative worker capabilities.
- Worker boundary kind.
- Request/response envelopes as contract-only shapes.
- Validator.
- Serializer.
- Fixtures.

## Not Implemented

- No worker runtime.
- No external process worker.
- No skill/package/recipe/step execution.
- No orchestration API.
- No UI.
- No persistence DB.
- No marketplace or package installation.

## Risk: Boundary vs Execution

The main risk is treating a registered/healthy worker as executable permission. V1 prevents that by requiring:

- `RuntimeExecutionAllowed=false`;
- `RuntimeExecutionDeferred=true`;
- `RequiresGlobalPolicyEvaluation=true`;
- `CanAuthorizeActions=false`;
- `InternalOnly=true`.

Health is diagnostic only. Request/response envelopes are conceptual contracts only.

## Temporary Location

Contracts live in `src/OneBrain.BrowserExecutor.Contracts/NodalOsWorkerBoundaryContracts.cs`.

Services live in `src/OneBrain.BrowserExecutor.Cdp/NodalOsWorkerBoundaryServices.cs`.

This follows the current temporary Agent Operations placement and remains a future extraction candidate.

## Naming

All new public symbols use `NodalOs*`. No new `Nexa*` types are introduced; `NexaFailureKind` is reused only as existing compatibility taxonomy.
