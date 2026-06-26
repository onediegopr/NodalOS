# CBPR-001/004 Perception Router Foundation Report

Date: 2026-06-26

Decision: `GO_CLOAK_BROWSER_PERCEPTION_ROUTER_FOUNDATION`

Line: `CLOAK_BROWSER_PERCEPTION_ROUTER_V1`

Initial HEAD: `3021836bae7c01f028f9a04a91928ab515b6dc83`

## What Changed

- Added `OneBrain.BrowserPerception` as a new isolated project.
- Added metadata-only contracts for browser perception snapshots, capability profiles, strategy routing, blockage placeholders, and evidence placeholders.
- Added fixture-safe `BrowserPerceptionSnapshotBuilder`.
- Added pure `PageCapabilityClassifier`.
- Added pure `StrategyRouter`.
- Added deterministic tests for the initial fixture matrix.
- Added inventory and architecture documentation.

## What Did Not Change

- No stealth-core/fingerprinter code changed.
- No protected isolated browser executor changed.
- No Bridge/WebSocket runtime changed.
- No browser launch hardening changed.
- No browser profile, proxy, header, user-agent, WebGL, canvas, or anti-detect behavior changed.
- No Chrome Extension legacy runtime changed.
- No CDP live action, injection, external navigation, or productive automation was added.

## Fixture Strategy Matrix

| Fixture | Expected Strategy | Status |
| --- | --- | --- |
| legacy form simple | `DOM_FIRST` | implemented |
| modern SPA-like page | `ACCESSIBILITY_FIRST` | implemented |
| iframe metadata | `FRAME_TARGET_REQUIRED` | implemented |
| shadow DOM marker | `SHADOW_DOM_REQUIRED` | implemented |
| canvas marker | `VISUAL_REQUIRED` | implemented |
| console critical error | `CONSOLE_DIAGNOSIS_REQUIRED` | implemented |
| network 500 failure | `NETWORK_DIAGNOSIS_REQUIRED` | implemented |
| network 401/403 auth failure | `HUMAN_HANDOFF_REQUIRED` | implemented |
| CAPTCHA/2FA/anti-bot marker | `HUMAN_HANDOFF_REQUIRED` | implemented |
| unknown page | `UNSUPPORTED_OR_HIGH_RISK` | implemented |

## Guardrail Results

| Guardrail | Result |
| --- | --- |
| No protected stealth/fingerprinter diff | PASS |
| No protected executor diff | PASS |
| No productive automation | PASS |
| No production injection | PASS |
| No external page action | PASS |
| CAPTCHA/2FA/anti-bot routes to human handoff | PASS |
| Metadata-only snapshot | PASS |

## Validation Notes

`dotnet restore .\OneBrain.slnx` was required once because this block added a new project. The required `--no-restore` build gate was then run after assets existed.

Final validation results are recorded in the JSON report and in the final task response.

## Risks

- Snapshot V0 is fixture-safe and not yet a live CDP collector.
- Locator Engine and Blockage Detector are not implemented in this block.
- Future live collectors must use adapter boundaries and must not modify protected executor/stealth internals.

## Next Recommended Block

`CBPR-005/006 — Locator Engine V1 + Blockage Detector V1`, still without productive actions.
