# NODAL OS Runner Filter Safe Commands Guidance

Date: 2026-07-08

Mode: docs-only / read-only / audit-only / operational-guidance-only.

Block: `AUTHORIZE_NODAL_OS_RUNNER_FILTER_HANG_OPERATIONAL_GUIDANCE_AND_SAFE_COMMANDS_DOCS_ONLY`.

Baseline HEAD: `d102de3e87f8bf0d6327f62e497600d4c4dfaa8c`.

Decision: `GO_WITH_FINDINGS_RUNNER_FILTER_SAFE_COMMAND_GUIDANCE_READY`.

Resulting state: `RUNNER_FILTER_SAFE_COMMAND_GUIDANCE_READY_NO_CI_ENFORCEMENT`.

Stop condition: `STOP_AFTER_RUNNER_FILTER_SAFE_COMMAND_GUIDANCE_READY_NO_CI_ENFORCEMENT`.

## Scope

This is local runner/test command guidance only.

No source, tests, project files, solution files, workflows, CI configuration, runtime/product code or infrastructure are changed by this block.

## Current Classification

Primary classification:

`WIDE_FILTER_UNSAFE_FOR_LOCAL_USE`

Secondary classification:

`LOCAL_RUNNER_FILTER_TIMEOUT_INTERMITTENT`

Current authority:

`docs/architecture/nodal-os-runner-filter-hang-investigation.md`

## Safe Command Rules

- Prefer focal filters over broad execution filters.
- Use explicit timeouts for every local `dotnet test` command in this lane.
- Use `-v:normal` when prior minimal/silent mode is suspect.
- Use `--list-tests` for broad discovery only.
- Do not use broad `FullyQualifiedName~ReentryDecisionPacketReadOnly` execution as a local gate.
- After any timeout, inspect and clean only residual `dotnet` / `vstest.console` processes that match the command started by the block.
- Limit retries to one controlled retry for an intermittent focal command.
- Do not infer CI failure from local runner/filter timeouts.

## Recommended Safe Commands

Local focal helper:

```powershell
pwsh -File tools/scripts/run-focal-dotnet.ps1 -Mode test -Project tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj -Filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests" -TimeoutSeconds 120
```

The helper is local/operator-run only. It does not enable CI enforcement, does not make broad filters safe as gates and still requires focal filters for `test` mode.
It also fails closed before `dotnet` when free disk space is below `-MinFreeGiB` (default `10`) or when the project path is outside the repository.

D7 focused:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyCommonBoundaryD7Tests" -v:normal
```

D8 focused:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyPostReplacementD8Tests" -v:normal
```

Reentry Safety:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlySafetyTests" -v:normal
```

Reentry Recipes:

```powershell
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnlyTests" -v:normal
```

StaticGuardCatalog:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~NodalOsStaticGuardCatalogTests" -v:normal
```

NoAuthority:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoAuthority" -v:minimal
```

NoDoubleTruth:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoDoubleTruth" -v:minimal
```

NoRuntimeWiring:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "TestCategory=NoRuntimeWiring" -v:minimal
```

Broad discovery/list-tests only:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --list-tests --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:normal
```

## Unsafe Or Not Recommended Commands

Do not use these as local gates:

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:minimal
```

```powershell
dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~ReentryDecisionPacketReadOnly" -v:normal
```

Avoid:

- broad `FullyQualifiedName` execution filters for this Reentry lane;
- broad `FullyQualifiedName~ReentryDecisionPacketReadOnly` variants with different casing or surrounding whitespace;
- broad silent/minimal execution filters that previously hung;
- suite-wide local execution without explicit timeout;
- repeating a timed-out command indefinitely;
- treating a local runner timeout as CI evidence.

## Timeout Guidance

Suggested windows:

- D7 focused: `90s`.
- D8 focused: `120s` because the source-scan allowlist test can be variable locally.
- Reentry Safety focused: `90s`.
- Reentry Recipes focused: `90s`.
- StaticGuardCatalog focused: `90s`.
- NoAuthority / NoDoubleTruth / NoRuntimeWiring: `240s`.
- Broad discovery/list-tests: `60s`.
- Broad execution: avoid.

Retry policy:

- max one retry for intermittent focal D8;
- no retry for broad execution filters in this lane;
- always clean block-owned residual processes after timeout before retrying anything.

## Cleanup Guidance

After a timeout, inspect local runner processes:

```powershell
Get-CimInstance Win32_Process -Filter "name = 'dotnet.exe'" |
    Select-Object ProcessId,CommandLine |
    Format-List
```

Cleanup must be narrow and attributable to the command started by the current block. Example pattern for a timed-out Reentry lane command:

```powershell
Get-CimInstance Win32_Process -Filter "name = 'dotnet.exe'" |
    Where-Object {
        $_.CommandLine -like '*ReentryDecisionPacketReadOnly*' -or
        $_.CommandLine -like '*vstest.console.dll*'
    } |
    ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
```

Do not use broad process cleanup if unrelated `dotnet` work may be running.

## What Must Not Be Inferred

- No CI failure is claimed.
- No CI enforcement is enabled.
- No runtime/product issue is proven.
- No source behavior regression is proven.
- No release/commercial implication is created.
- No test infrastructure fix is implemented.
- No source, tests, project files, solution files or workflows are changed.

## Findings

P0: 0.

P1: 0.

P2: 0.

P3:

- Broad Reentry execution filters remain unsafe for local gate use until a separate test-infra fix block is authorized.
- D8 focused execution remains locally variable and should use explicit timeout plus one controlled retry.

P4:

- Broad list-tests remains acceptable for discovery and matched-test inventory.

## Percentages

- Source-refactor readiness: `78%`.
- Test runner confidence: `74%` for focal filters, `35%` for broad local execution filters on this lane.
- D7 lane readiness: `100%`.
- Broad source simplification readiness: `45%`.
- Runtime/product enablement: `0%`.
- CI enforcement: `0%`.
- Release/commercial: `0% / NO-GO`.

## Next Step

Recommended next macro-block:

`NODAL_OS_SOURCE_REFACTOR_RETURN_TO_MAIN_ROADMAP_AFTER_RUNNER_GUIDANCE_AUDIT_ONLY`

Purpose:

Return source-refactor control to the main roadmap now that the D13/D7 micro-lane and runner guidance are recorded, and select the next non-runtime gate without relying on unsafe broad local filters.
