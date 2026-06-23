# M719A Worktree Hygiene Preflight

Decision: PRE_M719_CONTINUITY_GATE_READY

## Scope

Active project: NODAL OS.

NODRIX remains frozen and out of scope.

This block did not implement features and did not advance M719-M721. It only restored operational hygiene after M716-M718.

## Worktree Diagnosis

Initial M719A diagnosis found staged out-of-scope changes:

- `tests/OneBrain.Safety.Tests/BrowserConsentM21Tests.cs`
- asset/url retry artifacts from a different milestone line

The BrowserConsent diff changed `BrowserConsentExpiredGrantBlocksCapability` from a 1 ms TTL with inline `DateTimeOffset.UtcNow` to a fixed timestamp with 1 second TTL. That may be a valid stabilization in another block, but it did not belong to M716-M718 or M719A.

Resolution:

- reverted only `tests/OneBrain.Safety.Tests/BrowserConsentM21Tests.cs`;
- removed stray asset/url retry files;
- preserved NODAL OS M716-M718 committed work.

## Chrome Temp Profile Cleanup

The earlier full suite failure was environmental. The test `BrowserRuntimeSmokeCleanupLeavesNoManagedProcessPortOrProfile` detected locked `%TEMP%\\onebrain-cdp-*` profiles.

Diagnosis found lingering validation processes and Chrome headless processes using:

- `--headless=new`;
- `--remote-debugging-port`;
- `--user-data-dir` under `%TEMP%\\onebrain-cdp-*`.

Cleanup closed only test-owned processes linked to this repo or the `onebrain-cdp-*` temp profile. No general Chrome user process was closed.

Post-cleanup:

- remaining `onebrain-cdp-*` temp profiles: 0;
- remaining test-owned Chrome processes: 0.

## Validation

- `node --check browser-extension/onebrain-chrome-lab/service_worker.js`: PASS.
- `node --check browser-extension/onebrain-chrome-lab/sidepanel.js`: PASS.
- `dotnet build .\\OneBrain.slnx --no-restore`: PASS with historical warnings.
- targeted cleanup test `BrowserRuntimeSmokeCleanupLeavesNoManagedProcessPortOrProfile`: PASS.
- full suite first retry: failed on `FixtureCdpStepRunsUnderFsmAdapterAndCleansUp` due remote WebSocket close without close handshake.
- isolated rerun of failed test: PASS.
- final full suite rerun: PASS.

Final full suite:

- Recipes: 635 passed.
- Safety: 4888 passed, 37 skipped, 0 failed.

## Naming Continuity

NODAL OS remains the active project.

NODRIX references found by broad grep are historical, audit, or test guardrail references. No active product naming was reintroduced.

## Release Status

- Public package freeze: NO-GO.
- Public release: NO-GO.
- Chrome Web Store: NO-GO.

## Capability Status

- Runtime productive: disabled.
- Provider/cloud: disabled.
- Filesystem real access: disabled.
- Browser automation unlock: disabled.
- Capability unlock: disabled.

Bridge and CSP were not modified.

## Next Milestone

Recommended next milestone: M719-M721 Asset/URL Completion Retry.
