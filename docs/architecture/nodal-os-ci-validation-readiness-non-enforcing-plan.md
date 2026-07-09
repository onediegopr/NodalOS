# NODAL OS CI Validation Readiness Non-Enforcing Plan

Date: 2026-07-09

Mode: docs-only / design-only / no CI enforcement.

Decision: `GO_WITH_FINDINGS_CI_VALIDATION_READINESS_NON_ENFORCING_PLAN_READY`.

Resulting state: `CI_VALIDATION_READINESS_NON_ENFORCING_PLAN_READY_NO_CI_ENFORCEMENT`.

## Purpose

Increase validation reliability without adding workflows, enabling CI enforcement, opening runtime/product, or generating large artifacts.

This plan is a future-transition contract only. It does not make local validation equivalent to CI.

## Reliable Local Validation Today

- `tools/scripts/run-focal-dotnet.ps1` is the preferred local helper for bounded `dotnet build` and focal `dotnet test` commands.
- Safety/Core builds are reliable when run with `-m:1`, `BuildInParallel=false`, `UseSharedCompilation=false`, `-nr:false`, explicit timeout and build-server shutdown.
- Exact or narrow class filters are acceptable local evidence when they avoid known broad-filter hangs.
- `git diff --check`, docs-only scope scans, wiring scans and anti-overclaim scans are low-cost and should remain default validation for docs/design blocks.

## P3 Validation Risks

- Disk pressure is operational P3. C: reached `0` bytes free during the previous block, so validation must check free space before commands that can restore/build/download artifacts.
- Broad execution filters are P3 and must not be CI candidates until separately fixed.
- Local timeouts are runner evidence, not product failure evidence.
- Build/test commands can recreate `bin/`, `obj/`, package caches or model/runtime artifacts; blocks should avoid commands that generate large outputs unless they are essential.

## Candidate Future CI Shape

Future CI may be considered only after all preconditions below pass locally without large artifact growth:

- docs-only gate: `git diff --check`, docs scope scan, wiring scan, anti-overclaim scan;
- source/test focal gate: helper-shaped build plus exact/focal tests only;
- no broad filter execution gates;
- no release/commercial readiness gate;
- no runtime/product enablement gate;
- no external provider/cloud/network dependency;
- explicit disk-space guard before any build/test step.

## Disk-Space Policy

- Minimum free C: before local build/test validation: `10 GiB`.
- Recommended free C: `20 GiB` or more.
- If free space is below `10 GiB`, stop before build/test and clean only regenerable artifacts: `bin/`, `obj/`, `.tmp-*`, local caches, local virtualenvs and package installs.
- Do not delete tracked docs/source/tests/artifacts as part of validation cleanup unless a separate operator-approved cleanup block explicitly targets them.

## No-Broad-Filter Policy

- Broad filters are allowed for discovery/list-tests only.
- Broad filters are not gates.
- Known unsafe local execution filter remains blocked: `FullyQualifiedName~ReentryDecisionPacketReadOnly`.
- Any future workflow must start with exact tests or narrow class/category filters that have stable local evidence.

## Stop Conditions

- Any workflow/CI file edit is required.
- CI enforcement is requested.
- Runtime/product, public/product, release/commercial, DB/cloud/network/provider or KMS/WORM behavior is required.
- Free disk space falls below `10 GiB` and cannot be recovered with regenerable cleanup.
- A validation command needs broad filters, suite-wide execution or large artifact generation.
- A local timeout cannot be reduced to a focal helper command.

## Next Block Contract

Recommended next block:

`AUTHORIZE_NODAL_OS_CI_VALIDATION_READINESS_PACKET_REVIEW_READ_ONLY`

Scope:

- read-only/docs-only review of this plan;
- no workflow edits;
- no CI enforcement;
- no build/test execution unless helper help is needed;
- choose either a future docs-only CI packet or return to roadmap if no concrete next step exists.
