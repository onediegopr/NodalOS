# Package / Registry / Worker Integration No-Divergence Audit M401

Project: NODAL OS

Base commit: `7905b28`

## Searches Used

`Select-String` was used because `rg` was not available in the environment.

- `ValidateResponseEnvelope`
- `ResponseValues`
- `EvidenceRefs`
- `ValidateBridgeRef`
- `NodalOsEvidenceRefBridge`
- `BuildSnapshot`
- `BuildValidatedSnapshot`
- `RuntimeExecutionAllowed`
- `RuntimeExecutionDeferred`
- `RequiresGlobalPolicyEvaluation`
- `Name.Contains`
- `blocked`
- `deprecated`
- `NodalOsSkillCapabilityKind`
- `NodalOsWorkerCapabilityKind`
- `CanPassCatalogPolicy`
- `CanPassBoundaryPolicy`
- `Healthy`
- `Visible`

## Confirmed Findings

| Area | Finding | Risk | Decision |
| --- | --- | --- | --- |
| Worker response evidence | `ValidateResponseEnvelope` did not validate `EvidenceRefs` through `NodalOsEvidenceRefBridge.ValidateBridgeRef`. | A worker response could carry invalid evidence authority or redaction state across the boundary. | Integrate bridge validation inside worker response validation. |
| Worker response redaction scan | `ResponseValues` did not include evidence bridge fields. | Raw sensitive evidence refs could bypass worker response sanitizer checks. | Include safe evidence fields in the response redaction scan. |
| Registry snapshot builder | `NodalOsInternalSkillRegistryBuilder` copied runtime flags from package/skill manifests. | An unvalidated snapshot consumer could see runtime flags copied from an invalid source manifest. | Normalize registry entries to execution-deferred values and expose `BuildValidatedSnapshot`. |
| Registry status heuristic | `ValidateStatus` used `Name.Contains("blocked")` and `Name.Contains("deprecated")`. | Status semantics could be affected by display text. | Remove name-based status validation; status is enum-based only. |
| Capability mapping | Skill and worker capability enums overlap but had no explicit mapper. | Future consumers may infer mapping by enum name and miss drift. | Add `NodalOsWorkerSkillCapabilityMapper`. |
| Cross-layer tests | Package, registry, and worker tests were strong per layer but lacked integration no-divergence coverage. | Catalog/visibility/health flags could diverge without a failing test. | Add M401-M403 cross-layer no-divergence tests. |

## WorkerResponse Evidence Validation Gap

The response envelope is the first worker boundary object that can carry `NodalOsEvidenceBridgeRef` values. Those refs must be treated as evidence metadata only. They cannot grant action authority and cannot carry raw sensitive data without redaction.

Required behavior:

- validate every response `EvidenceRef` with `NodalOsEvidenceRefBridge.ValidateBridgeRef`;
- reject `RedactionRequired` and `RejectedSensitive`;
- reject sensitive evidence that is not redacted;
- keep errors safe and free of raw sensitive values;
- keep `Executed=false` and `RuntimeExecutionDeferred=true`.

## Registry Snapshot Validation Gap

`BuildSnapshot` existed as a raw builder output. M401-M403 keeps it for compatibility but adds a validated path. Registry entries are normalized to:

- `RuntimeExecutionAllowed=false`;
- `RuntimeExecutionDeferred=true`;
- `RequiresGlobalPolicyEvaluation=true`.

If a source package/skill declares unsafe runtime flags, the builder records a validation warning and still emits safe catalog metadata.

## Capability Mapping Gap

`NodalOsSkillCapabilityKind` and `NodalOsWorkerCapabilityKind` intentionally remain separate. M401-M403 adds a mapper instead of merging enums.

## Name.Contains Heuristic

The previous name-based checks were not security-critical, but they were a latent false-positive source. Registry status now depends on `NodalOsRegistryEntryStatus`, not text labels.

## Cross-Layer Invariant

Package manifest approval, registry visibility, worker health, `CanPassCatalogPolicy`, and `CanPassBoundaryPolicy` remain catalog/boundary semantics only. None grants runtime execution permission.

## What Changes

- Worker response validates evidence bridge refs.
- Worker response scans evidence bridge ref fields for sensitive content.
- Registry builder normalizes runtime flags.
- Registry builder exposes `BuildValidatedSnapshot`.
- Registry status validation stops using display names.
- Skill-to-worker capability mapper is explicit and fully tested.

## What Does Not Change

- No worker runtime.
- No recipe, skill, or step execution.
- No orchestration API.
- No UI.
- No registry persistence DB.
- No marketplace.
- No namespace move.
- No broad rename.

## Risks

The main remaining risk is future consumers bypassing `BuildValidatedSnapshot` and using raw `BuildSnapshot`. This is acceptable for compatibility, but next integration consumers should prefer validated snapshots.
