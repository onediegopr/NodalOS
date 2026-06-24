# M783 — Simulated Executable Orchestrator Boundary

**Decision:** SIMULATED_EXECUTABLE_ORCHESTRATOR_BOUNDARY_READY
**Project:** NODAL OS

## What was added

A minimal, safety-only, fake-only, in-memory orchestrator living entirely in the test project:

`tests/OneBrain.Safety.Tests/SimulatedDryRunOrchestrator.cs`

It is **not** product code, is **not** wired into the Bridge, the extension, or any `src` adapter, and performs no real side effect.

## Contract

```
input:  simulated request fixture
process:
  validate requestedMode == SIMULATED_DRY_RUN   (else DENY)
  validate fixtureType   == SIMULATED_FAKE_ONLY  (else DENY)
  if prohibited action -> DENY
  evaluate policy gate (in-memory)
  if manual approval required and not granted -> REQUIRE_MANUAL_APPROVAL
  else -> ALLOW_SIMULATED_DRY_RUN
  produce evidence envelope projection
  produce ledger event projection
  produce no-execution proof
output: ALLOW_SIMULATED_DRY_RUN / DENY / REQUIRE_MANUAL_APPROVAL
```

## The core guarantee — "no real executor wired"

The orchestrator receives an `ISimulatedSideEffectSink` **only so tests can prove non-invocation**. No branch of `Process(...)` ever calls any member of that sink. Therefore:

- **By construction:** there is no code path from any decision (including `ALLOW_SIMULATED_DRY_RUN`) to a real executor, provider client, filesystem writer, browser automation, capability unlock, public release, store submission, or signed ZIP creation.
- **By test:** a `RecordingSideEffectSink` is injected and asserted to record **zero** invocations across the ALLOW, DENY and REQUIRE_MANUAL_APPROVAL branches.

This converts the previously declarative M-1 enforcement into an executable, test-proven guarantee — still entirely simulated.

## Proof flags (all false)

`realExecutorInvoked` · `providerClientInvoked` · `filesystemWriterInvoked` · `browserAutomationInvoked` · `capabilityUnlockInvoked` · `publicReleaseInvoked` · `storeSubmissionInvoked` · `signedZipCreated` · `productFilesModified` · `bridgeCspModified`.

## Invariants preserved

Identical to M782: all runtime/release capabilities remain DISABLED / NO-GO; `PRODUCTIVE_ENABLED` PROHIBITED; product files / Bridge / CSP unchanged.
