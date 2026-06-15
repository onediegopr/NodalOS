# ADR: Browser Executor CDP-first contracts

## Status

Accepted for M1. This ADR defines contracts only. It does not migrate the current Chrome Operator, does not remove the MV3 service worker runner, and does not enable a production CDP executor.

## Context

The Browser-004.x hardening work improved the current Chrome Lab path, but also exposed structural limits:

- the MV3 service worker owns too much execution logic;
- run state is split between the local bridge and the extension;
- WebSocket/port liveness was treated as truth;
- there is no strong `TargetContext`;
- observation, action, verification, and evidence are informal;
- `uncertain` results can leak into success paths;
- browser-specific heuristics are mixed into runtime flow.

M0 validated the replacement spine as `M0 AMARILLO`: CDP/Playwright-first is viable with restrictions. The runtime can launch a controlled Chrome with CDP, list targets, observe URL/title/DOM/frame state, build a preliminary target context, perform trusted input on a local fixture, and detect stale targets. The main restriction is that attach to an already-open Chrome is only possible when Chrome was launched with remote debugging. Therefore ONE BRAIN needs an explicit Chrome Launcher / Browser Control Plane.

## Decision

Define formal Browser Executor contracts in `OneBrain.BrowserExecutor.Contracts`:

- `BrowserTargetContext`
- `BrowserObservation`
- `BrowserAction`
- `BrowserVerification`
- `BrowserEvidence`
- `BrowserHeartbeat`
- `BrowserIdempotencyLedger`
- `BrowserExecutorCapabilities`
- `ChromeLauncherPolicy`

These contracts are additive and inactive. They prepare M2 without changing existing Chrome Lab behavior.

## Runtime / Executor / Extension Boundary

The runtime is the authoritative owner of run state.

The Browser Executor performs browser observation and actions through CDP/Playwright. It reports observations, action attempts, verification results, liveness, capabilities, and evidence. It does not decide product policy by itself.

The Chrome extension remains UI plus relay/fallback during the transition. It is not the long-term brain and must not be the source of truth for target identity or run state.

## Contract Summary

### TargetContext

`TargetId + FrameId + Generation` form the basis of `LivenessToken`. `tabId` alone is not sufficient. A target can be invalidated by generation mismatch, target destruction, frame detach, or liveness failure. The contract supports main frames and subframes.

### Observation

An observation is read-only. It describes what the executor saw, not what it achieved. It has payload limits, sensitivity redaction flags, actionable element summaries, forms, links, warnings, and evidence references.

### Action

An action is an intent to affect or read a target. It requires an `ActionId`, a target context, an expected outcome, a timeout, and for modifying actions an `IdempotencyKey`. Critical actions require approval. An action attempt is not success.

### Verification

Verification determines whether an expected outcome happened. The rule is explicit:

```text
Uncertain != Done
```

Only `Verified` allows automatic step completion. `Uncertain`, `Failed`, and `Skipped` block automatic completion unless a future policy explicitly allows a skipped case.

### Evidence

Evidence is audit material referenced by observations and verifications. Inline payloads must not contain unredacted secrets. Sensitive evidence must mark redaction. Larger payloads should be stored by reference.

### Heartbeat / Liveness

Socket open is not alive. Strong `Alive` requires a target round-trip or a recent valid browser event. `Destroyed`, `Detached`, `Disconnected`, `Stale`, and `Unknown` do not permit productive actions.

### Idempotency / Replay Safety

Modifying actions use an idempotency key plus fingerprint. Exact duplicates are rejected or treated as cached. Same key with a different fingerprint is a failure. Completed actions do not execute again accidentally.

### Capability / Risk

The executor declares what it supports: trusted input, snapshots, frames, screenshots, profile modes, attach/launch requirements, and risk limit. Capability is not authorization; safety policy still gates actions.

### ChromeLauncherPolicy

CDP binds to localhost by default. Remote CDP is blocked by default. Real user profile use requires explicit consent. Sensitive logging redacts by default. Attach existing requires a configured remote debugging port.

## Alignment With Existing Runtime Concepts

M1 intentionally mirrors existing safety primitives instead of replacing them:

- `SafeExecutionFsm` remains the pattern for fail-closed execution state.
- `RecipeSafetyContract` remains the existing safety envelope for current recipe execution.
- `ElementIdentity` and `InvokeTimeIdentityGate` inform stale/identity checks.
- `EvidenceLedger` informs the evidence and transition audit model.
- `ApprovalBindingValidator` informs approval binding for critical browser actions.

M4 should connect Browser Executor contracts into FSM/Safety/Evidence so browser actions become first-class safe execution steps.

## Risks

- M2 still needs a real launcher and executor implementation.
- Real profile use can conflict with an already-running Chrome profile and needs explicit consent.
- Attach to arbitrary existing Chrome is not reliable without prior remote debugging setup.
- CDP target activation/visibility may require derived signals rather than one native field.
- The current extension-first runner remains until M3/M4 migration.
- Existing site-specific Chrome Lab heuristics remain technical debt until removed.

## Next Milestones

- M2: implement Browser Executor CDP-first inside the local runtime using these contracts.
- M3: integrate target selection, launcher policy, and liveness gates.
- M4: connect executor outcomes into FSM/Safety/Evidence and retire service-worker-owned execution.
