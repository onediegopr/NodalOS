# Package / Registry / Worker Integration No-Divergence M403

Project: NODAL OS

## Problem Resolved

M401-M403 closes the contract boundary between Package / Skill Manifest V1, Internal Skill Registry V1, and Worker Boundary Contract V1. The goal is no-divergence across catalog metadata, registry visibility, worker boundary policy, evidence handling, and runtime-permission invariants.

## Worker Evidence Validation

`NodalOsWorkerBoundaryValidator.ValidateResponseEnvelope` now validates each `NodalOsEvidenceBridgeRef` through `NodalOsEvidenceRefBridge.ValidateBridgeRef`.

Worker responses reject:

- evidence requiring redaction;
- rejected-sensitive evidence;
- sensitive evidence that is not redacted;
- evidence with worker-response authority beyond no-authority or diagnostic-only;
- raw sensitive content in evidence id, kind, ref, hash, ledger ref, or provenance.

Errors are diagnostic and do not include raw secret values.

## Registry Validated Snapshot

`NodalOsInternalSkillRegistryBuilder.BuildValidatedSnapshot` builds a registry snapshot and validates it through `NodalOsInternalSkillRegistryValidator`.

Registry entries are normalized to:

- `RuntimeExecutionAllowed=false`;
- `RuntimeExecutionDeferred=true`;
- `RequiresGlobalPolicyEvaluation=true`.

If source manifests declare unsafe runtime flags, the builder emits a warning and keeps the registry snapshot safe.

## Capability Mapper

`NodalOsWorkerSkillCapabilityMapper` maps every `NodalOsSkillCapabilityKind` to `NodalOsWorkerCapabilityKind`.

Enums remain separate because skill capabilities describe catalog skills and worker capabilities describe worker boundary declarations. The mapper makes that relationship explicit without collapsing the models.

## Registry Status Semantics

Registry status no longer depends on `Name.Contains("blocked")` or `Name.Contains("deprecated")`. Status is driven by `NodalOsRegistryEntryStatus`.

This avoids false positives where display text includes historical or helper wording.

## Cross-Layer Invariants

The new tests assert:

- package manifests do not grant runtime execution;
- registry entries do not grant runtime execution;
- worker boundary manifests do not grant runtime execution;
- registry visibility does not imply executability;
- worker health does not imply executability;
- `CanPassCatalogPolicy` does not imply executability;
- `CanPassBoundaryPolicy` does not imply executability;
- supported package/skill ids line up across registry entries and worker declarations.

## No Runtime / No Orchestration

This milestone remains contract-only.

Not implemented:

- worker runtime;
- skill execution;
- recipe execution;
- step execution;
- orchestration API;
- UI;
- registry persistence DB;
- marketplace;
- package installation.

## Next Steps

Recommended next milestone: `M404-M406 Agent Operations Extraction Prep` or `M404-M406 Orchestration API Decision Record`.

Extraction prep is still safer before any executable orchestration surface because the Package / Registry / Worker contracts remain temporarily hosted under `OneBrain.BrowserExecutor.*`.
