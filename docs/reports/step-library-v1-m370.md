# Step Library V1 M370

## Problem

Recipes and run reports need a shared, governed vocabulary for step types, risk metadata, policy metadata, and failure mapping before execution or orchestration expands.

## Axiom-Style Takeaway

The useful idea is a step library: a stable catalog of steps with metadata. NODAL OS adapts it as core-owned governance data, not as no-code execution authority.

## Step Kinds V1

- Navigate
- Read
- Click
- Type
- Extract
- Wait
- AskHuman
- Stop
- DownloadRequest
- UploadRequest

## Risk and Capability Metadata

Every step has:

- risk level
- capability list
- read-only capability flag
- default approval flag
- sensitivity flag
- V1 allowance flag
- failure taxonomy mapping
- evidence requirements

## Approval Metadata

Download and upload requests require approval. Sensitive typing requires approval and is blocked when global sensitive action blocking is active.

## Read-Only vs Interaction

Read, Extract, Wait, AskHuman, and Stop are read-only or control-flow capable. Click, Type, UploadRequest, and DownloadRequest are interaction or file-transfer steps and require stronger policy handling.

## Recipe Manifest Mapping

`NodalOsRecipeActionKind` maps one-to-one to `NodalOsStepKind`. Recipe Manifest policy still dominates.

## Run Report Mapping

Step kinds map to stable lower-kebab action strings for `NexaRunStepReport.ActionKind`.

## Failure Taxonomy Mapping

Definitions and validator results include `NexaFailureKind` mappings such as `PolicyBlocked`, `ApprovalRequired`, `SensitiveDataRisk`, `DownloadBlocked`, `UploadBlocked`, `LoginRequired`, `CaptchaDetected`, and `TwoFactorRequired`.

## Excluded From V1

Submit, pay, delete, publish, send, sign, login automation, captcha automation, and 2FA automation are not normal Step Library V1 steps. They are blocked by validation context and mapped to failure taxonomy.

## Non-Goals

- No step execution.
- No browser actions.
- No recipe execution.
- No orchestration API.
- No scheduled runs.
- No UI or sidepanel.
- No approval UI.
- No cloud runtime.

## Next Step

Recommended next milestone: `M371-M373 Core Legacy Reference Graph or Claude Agent Operations Audit`.
