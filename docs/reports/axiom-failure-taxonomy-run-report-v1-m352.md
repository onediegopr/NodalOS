# Axiom Failure Taxonomy Run Report V1 M352

## Summary

M350-M352 implements the first Agent Operations reporting layer inspired by Axiom-style run reports, without adding UI, orchestration API, scheduled runs, recipe execution or browser actions.

## What Was Taken From Axiom

- typed run reports
- typed failure taxonomy
- troubleshooting recommendations
- blocked, failed and successful run fixtures
- evidence references attached to report entities

## Failure Taxonomy

`NexaFailureKind` covers selector issues, navigation failures, login/captcha/2FA boundaries, policy blocks, approval requirements, progress loops, runtime failures, transfer blocks, sensitive-data risk and unknown failures.

The taxonomy is separate from the existing executor-level `FailureKind`. This block creates an Agent Operations reporting model, not a runtime execution model.

## Troubleshooting Mapper

`NodalOsTroubleshootingRecommendationMapper` provides a recommendation for every `NexaFailureKind`.

Sensitive and human-gated conditions fail closed:

- captcha requires human input and no automatic retry
- two-factor authentication requires human input and no automatic retry
- login requires human input
- policy blocked does not auto-retry
- repeated action stops the loop
- sensitive data risk stops or requires approval according to policy

## Run Report V1

`NexaRunReport` records:

- RunId
- MissionId / TaskId / RecipeId
- goal
- run status
- step reports
- policy decisions
- approvals
- failures
- evidence references
- final summary

The builder creates successful, blocked-by-policy and failed fixtures.

## Relation To Mission / Task

Run reports can attach to the M347-M349 Mission / Task domain through `MissionId`, `TaskId` and `NexaEvidenceRef`.

## Relation To Recipe Manifest

`RecipeId` is included for M353-M355 Recipe Manifest / Automation JSON V1. No recipe execution is introduced in this block.

## Relation To Sidepanel / Timeline

Future UI can render these reports, but UI remains non-authoritative. Core policy and evidence remain the source of truth.

## Sanitization

Run reports are validated to reject sensitive strings such as cookies, authorization headers, bearer tokens, passwords, secrets and token-like fields.

## Non-Goals

- no run report UI
- no sidepanel changes
- no timeline changes
- no orchestration API
- no real run execution
- no scheduled runs
- no recipe manifest implementation
- no step library
- no package registry
- no browser actions
- no persistence database

## Decision

`M350+M351+M352 CERRADO / RUN_REPORT_AND_FAILURE_TAXONOMY_READY`
