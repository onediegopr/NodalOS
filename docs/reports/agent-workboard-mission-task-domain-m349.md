# Agent Workboard Mission Task Domain M349

## Summary

M347-M349 creates the Core-owned Mission / Task domain model for Agent Operations and the future Agent Workboard.

The block introduces no UI, no orchestration API, no runtime execution and no browser actions.

## Models

- `NexaMission`
- `NexaAgentTask`
- `NexaProgressNote`
- `NexaBlockerReport`
- `NexaVerificationCheck`
- `NexaEvidenceRef`
- `NexaTaskValidationResult`

## Validation Rules

- Mission id, title and human owner are required.
- Task id, mission id, title and human owner are required.
- Completed tasks require verification before done.
- Completed tasks require at least one evidence reference or explicit completion reason.
- Blocking and critical blockers prevent completion.
- Pending and failed required verification checks prevent completion.
- Skipped required verification is acceptable only when a detail reason is present.
- Cancelled tasks without a completion reason produce a warning.
- Failed tasks should preserve blocker, evidence or failure reason.

## Relation To Agent Workboard

The model gives the future Agent Workboard a Core-owned task structure. UI can render missions, tasks, progress notes, blockers, verification checks and evidence references, but it does not decide task completion.

## Relation To Run Report V1

Future Run Report V1 can attach run evidence to `NexaEvidenceRef` and verification checks. Task closure can then reference build, test, audit or runtime reports without making the report itself authoritative.

## Relation To Recipe Manifest V1

Future Recipe Manifest V1 can link planned recipe steps to mission tasks, while task completion remains governed by validation and evidence rules.

## Relation To Failure Taxonomy

`NexaBlockerReport` is intentionally aligned with future typed failures. Blocking and critical severity already participate in close validation.

## Non-Goals

- no workboard UI
- no new sidepanel
- no new timeline
- no orchestration API
- no run execution
- no scheduled runs
- no package registry
- no multi-worker runtime
- no cloud runtime
- no browser actions
- no approval UI
- no recipe execution
- no persistence database

## Decision

`M347+M348+M349 CERRADO / MISSION_TASK_DOMAIN_READY`
