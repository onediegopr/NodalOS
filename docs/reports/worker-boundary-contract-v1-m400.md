# NODAL OS Worker Boundary Contract V1 M400

## Problem

NODAL OS needs a governed worker boundary contract before any future worker runtime or orchestration API exists. The contract must define identity, capabilities, policy requirements, evidence/redaction boundaries, health reporting, and request/response shapes without executing skills.

## Worker Boundary vs Worker Runtime

Worker Boundary V1 is descriptive and governance-only. It declares what a future worker boundary claims to support and how it must be evaluated.

Worker Runtime is future executable infrastructure. It is not implemented in M398-M400.

## Worker Status

- `Draft`
- `Registered`
- `Disabled`
- `Deprecated`
- `Blocked`

Only `Registered` can pass boundary policy, and even then it does not grant runtime permission.

## Worker Health

Health statuses:

- `Unknown`
- `Healthy`
- `Degraded`
- `Unhealthy`
- `Offline`

Healthy means diagnostic health only. It does not imply permission to execute.

## Capability Declaration

Capabilities are declarative:

- `ReadOnly`
- `Navigation`
- `Extraction`
- `Reporting`
- `EvidenceProcessing`
- `HumanInput`
- `FileTransfer`
- `DataEntry`
- `Interaction`
- `ControlFlow`
- `Unknown`

Capabilities do not authorize actions.

## Request / Response Envelopes

Worker request envelopes are contract-only and require:

- `ExecutionDeferred=true`;
- `RequiresGlobalPolicyEvaluation=true`.

Worker response envelopes are contract-only and require:

- `Executed=false`;
- `RuntimeExecutionDeferred=true`.

Responses can carry `NodalOsEvidenceBridgeRef` and `NexaFailureKind` values for reporting, not for action authorization.

## Runtime Execution Deferred

Worker Boundary V1 requires:

- `RuntimeExecutionAllowed=false`;
- `RuntimeExecutionDeferred=true`;
- `RequiresGlobalPolicyEvaluation=true`;
- `CanAuthorizeActions=false`;
- `InternalOnly=true`.

## Relationships

Package / Skill Registry:

Workers can declare supported package IDs and skill IDs, but lookup/support does not imply execution.

Evidence Bridge:

Worker responses can carry no-authority bridge refs.

Failure Taxonomy:

Worker responses can report typed failure kinds.

Common Redaction:

Worker manifests, health reports, requests, and responses reject secret-like content through `NodalOsRedactionService`.

## Not Implemented

- No worker runtime.
- No external process worker.
- No skill execution.
- No recipe execution.
- No step execution.
- No orchestration API.
- No UI.
- No persistence DB.
- No marketplace.
- No package install/uninstall.

## Next Steps

Recommended next milestone: `M401-M403 Agent Operations Extraction Prep or M401-M403 Orchestration API Decision Record`.
