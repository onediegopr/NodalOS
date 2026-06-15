# ADR: Browser Extension Companion Core-Governed Mode (M5)

## Context

The original Chrome lab extension accumulated too much execution ownership inside the MV3 service worker:

- local run and recipe state;
- a recipe runner loop;
- tab routing and content-script action forwarding;
- local success states derived from tool responses;
- WebSocket connection state mixed with runtime liveness.

M1-M4 moved the future browser execution spine into formal contracts, a CDP executor, smoke gates, and an FSM/Safety/Evidence adapter. M5 starts the surgical reduction of the extension so it can remain useful without being the authoritative run brain.

## Decision

The Chrome extension is now treated as a core-governed companion:

- it may connect, relay, observe, report health, and surface status;
- it may provide fallback content-script sensing;
- it must not own final run success;
- it must not treat a content-script response as verification;
- it must not restore or run the legacy recipe runner by default.

The service worker declares:

- `EXTENSION_RUNTIME_MODE = "core-governed-companion"`;
- `CORE_GOVERNED_MODE = true`;
- `LEGACY_RUNNER_ENABLED = false`;
- capabilities that explicitly mark `serviceWorkerRunOwner`, `canVerifyFinalSuccess`, and `contentScriptAuthoritative` as false.

## Ownership Removed

- The legacy recipe runner is disabled by default.
- Persisted `recipeRunner` state is not restored in core-governed mode.
- Runtime snapshots expose the companion/core-governed capability flags.
- Extension `tool.result` messages are marked as relay results, non-authoritative, and `NotVerified`.
- Incoming `run.status = completed` is downgraded to `uncertain` unless a strong `Verified` verification status is present.

## What Remains Legacy

The service worker still contains the old recipe runner implementation behind the disabled flag. It is intentionally not deleted in M5 because the extension still has existing UI and storage flows that need a controlled migration path.

The content script remains available as a sensor/fallback transport. It must not become the authoritative executor for future core-governed browser runs.

The Chrome Lab bridge remains an extension transport and diagnostic bridge. The CDP Browser Executor remains the core-governed runtime path introduced in M2-M4.

## Protocol Boundary

Extension-to-core messages must carry enough identity to avoid ambiguous success:

- `runId`;
- `requestId`;
- `actionId`;
- `correlationId`;
- `runtimeKind`;
- `source`;
- `authoritative`;
- `verificationStatus`;
- `evidenceRefs`;
- `redacted`.

Relay accepted is not done. Socket connected is not alive. Tool responded is not verified. `Uncertain` is not success.

## Out Of Scope

M5 does not:

- migrate the side panel UX;
- remove the entire legacy runner code path;
- automate external sites;
- implement profile/session management;
- implement downloads/uploads;
- implement WebView2 or CEF;
- implement a full human-handoff UI.

## Risks

- Existing UI entry points that attempted to run recipes from the extension now receive a blocked/core-governed status until they are routed through the core executor.
- Some dirty pre-M5 extension changes are still present in the branch and must be separated in later cleanup work.
- The service worker still contains legacy code, so future changes must preserve the disabled-by-default invariant.

## Next Step

M6 should run the first site-real read-only gate through the core-governed Browser Executor path, not through the extension recipe runner. The extension should only display status, relay diagnostics, and provide fallback observations when explicitly requested by core policy.
