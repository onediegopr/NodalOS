# Workitem Queue Contract

## Purpose

The workitem queue contract captures the useful OpenRPA/OpenCore pattern of queued items with payloads, attachments, priority, next-run metadata, processing state, retry state, and explicit failure classification.

This is a contract-only model. It does not implement a scheduler, file watcher, background worker, recorder, replay engine, browser automation, desktop automation, network provider, or live execution loop.

## Statuses

- `New`
- `Ready`
- `Processing`
- `Succeeded`
- `RetryScheduled`
- `FailedBusiness`
- `FailedApplication`
- `FailedPolicy`
- `FailedValidation`
- `NeedsHuman`
- `Cancelled`
- `Skipped`

## Failure Types

- `Business`
- `Application`
- `Policy`
- `Validation`
- `Perception`
- `Locator`
- `Auth`
- `Challenge`
- `Timeout`
- `RateLimit`
- `ExternalSystem`
- `Unknown`

Business failures are non-retryable by default. Application, timeout, rate-limit, and external-system failures are retryable only when policy allows. Policy, auth, and challenge failures require human intervention or remain blocked.

## Safe Fields

Workitems carry payload and attachment refs:

- payload json may be redacted fixture payload,
- attachments are evidence refs,
- secret references are ids only,
- no raw attachment bytes,
- no clipboard content,
- no live runtime flags,
- no action authority.

## Lifecycle Pattern

The future main workflow pattern is represented as:

1. Pop item.
2. Process item.
3. Update state.
4. Handle business/application/policy/validation failures.
5. Schedule retry by metadata only, not by a live timer.
6. Handoff to a human when required.
