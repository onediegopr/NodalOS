# M782 — Simulated Runtime Boundary Implementation Plan

**Decision:** SIMULATED_RUNTIME_BOUNDARY_IMPLEMENTATION_PLAN_READY
**Project:** NODAL OS
**Branch:** chrome-lab-001-extension-local-ai-bridge
**Prior:** M779-M781 CLOSED (roundtrip audit summary) → Claude external audit `AUDIT_GO / CONTINUE_TO_M782_M784`

## Goal

Address the persistent MEDIUM finding **M-1** ("defence is data-driven contracts/tests, not yet executable enforcement") by introducing a **fake-only, in-memory** simulated runtime boundary that is *executable*, while keeping every runtime/release invariant intact.

## Runtime type

`SIMULATED_FAKE_ONLY_IN_MEMORY`. No real executor, provider client, filesystem writer, browser automation adapter, capability unlock executor, release procedure, or store submission procedure is wired.

## In scope

- Fake-only, in-memory boundary that accepts only `requestedMode = SIMULATED_DRY_RUN` and `fixtureType = SIMULATED_FAKE_ONLY`.
- In-memory policy gate + manual approval evaluation.
- Decision production: `ALLOW_SIMULATED_DRY_RUN` / `DENY` / `REQUIRE_MANUAL_APPROVAL`.
- Simulated evidence envelope, ledger event projection, and no-execution proof per request.

## Out of scope (disallowed paths)

real executor · provider/cloud client · filesystem writer · browser automation adapter · capability unlock executor · public release procedure · Chrome Web Store submission procedure · signed ZIP creation · product files mutation · Bridge/CSP mutation.

## Implementation location

`tests/OneBrain.Safety.Tests/` only (safety-only test helper). **No product files, no Bridge/CSP, no src product adapters.**

## Invariants preserved

Runtime productive execution DISABLED · provider/cloud live calls DISABLED · filesystem/browser/capability unlock DISABLED · `PRODUCTIVE_ENABLED` PROHIBITED · Public Release NO-GO · Chrome Web Store NO-GO · product files / Bridge / CSP unchanged.

## Next

M783 implements the executable simulated orchestrator boundary and proves no real executor is wired.
