# ADR M45: Release and Update Channel Model

## Status

Accepted for M45.

## Context

The product requires release/update planning, but real auto-update, remote executable updates, and binary download/execution remain prohibited.

## Decision

M45 introduces model-only release and update contracts:

- release channels;
- release versions;
- update manifests;
- package descriptors;
- integrity descriptors;
- compatibility checks;
- rollback plans;
- eligibility evaluator;
- release audit events.

Channels:

- Dev;
- Internal;
- Preview;
- Beta;
- Stable;
- EnterprisePinned;
- Disabled.

## Update Manifest

The update manifest requires:

- target version;
- channel;
- component list;
- runtime/OS/browser compatibility assumptions;
- hash;
- signature metadata placeholder;
- rollback target;
- compatibility notes.

It does not download or execute binaries.

## Eligibility

`NexaUpdateEligibilityEvaluator` blocks:

- disabled channel;
- target channel mismatch;
- enterprise pinned mismatch;
- missing hash/signature metadata;
- runtime incompatibility;
- missing admin approval when required;
- tenant policy bypass;
- requested auto-execution.

## Rollback

Rollback is model-only. It can produce a rollback decision but never executes rollback.

## Out of Scope

M45 does not implement:

- real auto-update;
- update download;
- binary execution;
- remote update service;
- signing infrastructure;
- executable rollback.

## Consequences

Release readiness now has explicit channel, compatibility, integrity, approval, and rollback models without introducing executable update risk.
