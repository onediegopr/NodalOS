# NODAL OS Package / Skill Manifest V1 M394

## Problem

NODAL OS needs a governed way to describe internal packages and skills before introducing registries, workers, orchestration, or UI. The manifest must support provenance, versioning, risk metadata, approval requirements, allowed domains, and evidence requirements without granting runtime permission.

## What Package / Skill Manifest V1 Adds

- `NodalOsPackageManifest` for internal package metadata.
- `NodalOsSkillManifest` for governed skill metadata.
- package and skill status enums.
- skill capability and risk enums.
- JSON serializer.
- validator.
- fixtures for safe and unsafe states.

## Package vs Skill vs Recipe vs Step vs Registry vs Worker

| Concept | V1 meaning |
| --- | --- |
| Package | Internal catalog container for one or more skills with publisher, provenance, version, and tags. |
| Skill | Reusable governed capability metadata with risk, approvals, evidence, and optional recipe/step links. |
| Recipe | Automation definition and local recipe policy. |
| Step | Primitive catalog metadata for individual step kinds. |
| Registry | Future discovery/index service. Not implemented. |
| Worker | Future execution boundary. Not implemented. |

## Statuses

Package statuses:

- `Draft`
- `InternalPreview`
- `ApprovedForCatalog`
- `Deprecated`
- `Blocked`

Skill statuses:

- `Draft`
- `InternalPreview`
- `ApprovedForCatalog`
- `Deprecated`
- `Blocked`

`ApprovedForCatalog` means catalog governance only. It does not grant execution.

## Runtime Execution Deferred

Both package and skill manifests require:

- `RuntimeExecutionAllowed=false`
- `RuntimeExecutionDeferred=true`
- `RequiresGlobalPolicyEvaluation=true`

This mirrors the M386-M388 Recipe/Step runtime-permission cleanup. Package/Skill Manifest V1 cannot authorize runtime actions.

## Internal-Only V1

`InternalOnly=true` is required for both packages and skills in V1. External marketplaces, cloud runtime, package installation, and public registries are out of scope.

## Global Policy Required

Catalog policy only determines whether metadata can enter the internal catalog. Global policy remains authoritative for any future runtime use.

## Approval Requirements

- High and Critical risk skills require approval metadata.
- FileTransfer requires approval metadata.
- DataEntry requires approval metadata.
- Interaction without approval metadata is warned before runtime integration.

## Evidence Requirements

Packages and skills can declare evidence requirements. These are preserved through serialization and validation but do not create or persist evidence.

## Redaction and Sensitive Handling

The validator uses `NodalOsRedactionService` / `NodalOsSensitiveContentClassifier` to reject secret-like content in manifest fields, tags, provenance, evidence requirements, approvals, related recipe IDs, and related step kinds.

## What This Does Not Implement

- No registry.
- No worker runtime.
- No marketplace.
- No package installation.
- No UI.
- No orchestration API.
- No recipe execution.
- No step execution.
- No connector or external tool execution.
- No persistence DB.
- No namespace move.

## Next Steps

Recommended next milestone: `M395-M397 Internal Skill Registry V1 Design or Agent Operations Extraction Prep`.
