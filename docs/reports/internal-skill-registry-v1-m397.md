# NODAL OS Internal Skill Registry V1 M397

## Problem

After Package / Skill Manifest V1, NODAL OS needs a governed way to index package and skill metadata for internal catalog lookup. The registry must preserve provenance, evidence requirements, policy metadata, and runtime-deferred semantics without adding persistence or execution.

## Manifest vs Registry

Manifests are source metadata. The registry is a catalog snapshot derived from manifests.

The registry does not replace manifest validation. It indexes validated metadata and applies registry-level checks for duplicates, visibility, sensitive content, runtime-permission invariants, and catalog status.

## Registry V1

Internal Skill Registry V1 includes:

- `NodalOsSkillRegistryEntry`;
- `NodalOsInternalSkillRegistrySnapshot`;
- `NodalOsSkillRegistryQuery`;
- builder from package manifests;
- validator;
- query service;
- JSON serializer.

## Entry Status

- `Draft`
- `Visible`
- `Hidden`
- `Deprecated`
- `Blocked`

Visible means catalog-visible only. It does not imply execution permission.

## Query

The query service supports:

- find by package ID;
- find by skill ID;
- filter by entry kind;
- filter by status;
- filter by max risk level;
- filter by required capabilities;
- visible-only lookup.

## Runtime Execution Deferred

Registry entries must preserve:

- `RuntimeExecutionAllowed=false`;
- `RuntimeExecutionDeferred=true`;
- `RequiresGlobalPolicyEvaluation=true`;
- `InternalOnly=true`.

The registry cannot grant runtime permission.

## Provenance, Evidence, and Policy Metadata

Package provenance is preserved on package and skill entries. Evidence requirements are preserved from package and skill manifests. Risk and capability metadata are preserved on skill entries.

## Redaction

The registry validator uses `NodalOsRedactionService` to reject secret-like values in entry metadata. Raw cookies, headers, tokens, passwords, and private bodies are not allowed.

## Not Implemented

- No persistence DB.
- No worker runtime.
- No marketplace.
- No package installation.
- No skill execution.
- No recipe execution.
- No step execution.
- No UI.
- No orchestration API.

## Next Steps

Recommended next milestone: `M398-M400 Worker Boundary Contract V1 or Agent Operations Extraction Prep`.
