# NODAL OS Runner Filter Hang Investigation - Local/Test-Infra Audit Only

Date: 2026-07-08

Mode: read-only / audit-only / docs-only / test-infra-audit-only.

Block: `AUTHORIZE_NODAL_OS_RUNNER_FILTER_HANG_INVESTIGATION_READ_ONLY_OR_TEST_INFRA_AUDIT_ONLY`.

Baseline HEAD: `f934358b9148d73e8f968c0be2dfd9189f61511e`.

Decision: `GO_WITH_FINDINGS_RUNNER_FILTER_HANG_INVESTIGATION_RECORDED`.

Resulting state: `RUNNER_FILTER_HANG_INVESTIGATION_RECORDED_NO_CI_ENFORCEMENT`.

Stop condition: `STOP_AFTER_RUNNER_FILTER_HANG_INVESTIGATION_RECORDED_NO_CI_ENFORCEMENT`.

## Scope

This block investigated local `dotnet test` filter behavior only.

No source, tests, project files, solution files, workflows, CI configuration, runtime/product code or infrastructure were modified.

## Prior Evidence

The D7 micro-lane recorded a P3 runner/filter finding:

- broad Safety filter `FullyQualifiedName~ReentryDecisionPacketReadOnly` hung without output and was stopped;
- D8 focused run with `-v:minimal` previously timed out;
- the same D8 class with `-v:normal` passed in a later run;
- D7, D8, Reentry Safety, Reentry Recipes, StaticGuardCatalog, NoAuthority, NoDoubleTruth and NoRuntimeWiring focused runs passed;
- there was no evidence of D7 equivalence failure.

## Command Matrix

| Label | Command | Timeout | Expected behavior | Observed behavior | Result | Notes |
| --- | --- | ---: | --- | --- | --- | --- |
| A. D7 focused normal | `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests" -v:normal` | 90s | Pass focused D7 tests | Passed 12/12 in 59.5078s test time, 67.92s elapsed | PASS | Longest test was source-scan heavy but completed |
| B. D8 focused normal | `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyPostReplacementD8Tests" -v:normal` | 90s | Pass focused D8 tests | Timed out and left dotnet/vstest processes | TIMEOUT | One retry was allowed |
| B retry. D8 focused normal | same as B | 120s | Pass focused D8 tests | Passed 10/10 in 31.7890s test time, 48.03s elapsed | PASS | Confirms intermittent local runner/process behavior |
| C. Reentry Safety normal | `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlySafetyTests" -v:normal` | 90s | Pass focused Reentry Safety tests | Passed 6/6 in 15.0954s test time, 24.00s elapsed | PASS | No equivalence concern |
| D. Reentry Recipes normal | `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyTests" -v:normal` | 90s | Pass focused Reentry Recipes tests | Passed 4/4 in 4.3796s test time, 29.63s elapsed | PASS | No recipes concern |
| E. Broad Reentry minimal | `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:minimal` | 60s | Either pass or reproduce hang | Timed out and left dotnet/vstest processes | TIMEOUT | Reproduces prior broad/silent filter issue |
| F. Broad Reentry normal | `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:normal` | 120s | Determine whether verbosity fixes broad filter | Timed out and left dotnet/vstest processes | TIMEOUT | Verbosity alone does not make broad execution safe |
| G. Broad Reentry list tests | `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --list-tests --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:normal` | 60s | List matched tests without executing suite | Listed 28 tests and completed in 17.53s | PASS | Discovery/listing is safe under this filter |

## Classification

Primary classification:

`WIDE_FILTER_UNSAFE_FOR_LOCAL_USE`

Secondary classification:

`LOCAL_RUNNER_FILTER_TIMEOUT_INTERMITTENT`

Rationale:

- The broad execution filter timed out under both `-v:minimal` and `-v:normal`.
- The broad list/discovery command completed and showed 28 matched tests.
- Focused class filters passed, though D8 showed one timeout followed by one successful retry.
- Timeouts left `dotnet` and `vstest.console` processes alive until explicitly cleaned up.
- No test assertion failures were observed.

## Recommendation

Use focal filters only for this lane until a separately authorized test-infra fix block exists.

Recommended safe pattern:

- prefer concrete class filters such as `ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests`, `ReentryDecisionPacketReadOnlyPostReplacementD8Tests`, `ReentryDecisionPacketReadOnlySafetyTests` and `ReentryDecisionPacketReadOnlyTests`;
- run source-scan-heavy focused tests with explicit timeouts and process cleanup;
- use `--list-tests` for broad discovery only;
- avoid executing broad `FullyQualifiedName~ReentryDecisionPacketReadOnly` locally as a validation gate;
- do not infer CI failure from this local-only audit;
- require a separate test-infra fix block before changing tests, project files, runner configuration or CI.

## What Must Not Be Inferred

- No product/runtime issue was proven.
- No source behavior regression was proven.
- No D7 equivalence failure was proven.
- No CI failure was claimed.
- No CI enforcement was enabled.
- No release/commercial implication was created.
- No source, tests, project files, solution files or workflows were changed.

## Process Cleanup

Timed-out broad commands left parent `dotnet` and child `vstest.console` processes. The investigation explicitly cleaned only processes matching the command filters started by this block. Final process scan found no remaining `dotnet.exe` processes.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Wide execution filter `FullyQualifiedName~ReentryDecisionPacketReadOnly` is unsafe for local validation use because it timed out under both minimal and normal verbosity and left runner processes alive.
- D8 focused execution can be locally intermittent and should use explicit timeout plus one controlled retry when needed.

P4:

- Broad discovery/listing is safe for the audited filter and can be used to inspect matched test count without executing the broad set.

## Percentages

- Source-refactor readiness: `78%`.
- Test runner confidence: `72%` for focal filters, `35%` for broad local execution filters on this lane.
- D7 lane readiness: `100%`.
- Broad source simplification readiness: `45%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Next Step

Recommended next macro-block:

`NODAL_OS_RUNNER_FILTER_HANG_OPERATIONAL_GUIDANCE_AND_SAFE_COMMANDS_DOCS_ONLY`

Purpose:

Record a concise operator-safe command policy for this lane so future source-refactor blocks use focal filters, explicit timeouts and cleanup rules without treating the broad local filter as CI evidence.
