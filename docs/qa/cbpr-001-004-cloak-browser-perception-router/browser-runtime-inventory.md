# CBPR-001/004 Browser Runtime Inventory

Date: 2026-06-26

Project: NODAL OS

Branch: `chrome-lab-001-extension-local-ai-bridge`

Initial HEAD: `3021836bae7c01f028f9a04a91928ab515b6dc83`

Mode: read-only inventory before `CLOAK_BROWSER_PERCEPTION_ROUTER_V1`.

## Summary

This inventory reviewed the browser runtime and CDP migration surface to define a safe boundary for a new perception router foundation. The implementation boundary is a new project, `src/OneBrain.BrowserPerception`, with no edits to stealth core, fingerprinter, protected isolated browser execution, launch hardening, runtime profiles, browser profiles, proxy, headers, user-agent, WebGL, canvas, Bridge/WebSocket, or Chrome Extension legacy runtime.

## Classification

| Area | Classification | Paths | Notes |
| --- | --- | --- | --- |
| CloakBrowser runtime lock/guard | REUSE | `browser-runtime.lock.json`, `src/OneBrain.BrowserRuntime/*Runtime*`, `src/OneBrain.BrowserRuntime/*Guard*` | Existing runtime pinning and anti-system-browser proof remains the source of truth. |
| CDP Browser Skills read-only foundation | REUSE | `src/OneBrain.BrowserRuntime/CdpBrowserSkills.cs`, `src/OneBrain.BrowserRuntime/CdpBrowserSkillsSession.cs`, `src/OneBrain.BrowserRuntime/CdpDirectContracts.cs` | Future perception collectors can adapt through contracts; this block did not invoke live CDP. |
| Safe local status/evidence channel | REUSE | `src/OneBrain.BrowserRuntime/*SafeLocalStatus*`, `scripts/export-cloakbrowser-cdp-ui-status-snapshot.ps1` | Evidence remains metadata-only and redacted. |
| Isolated browser executor | READ_ONLY_PROTECTED | `src/OneBrain.BrowserExecutor.Cdp/BrowserCredentialBoundaryService.cs`, `src/OneBrain.BrowserExecutor.Contracts/BrowserCredentialBoundaryContracts.cs` | Protected by post-M1345 scope. No changes made. |
| Companion Bridge/WebSocket/Stealth | READ_ONLY_PROTECTED | `src/OneBrain.ChromeLab.Bridge/Program.cs`, `src/OneBrain.ChromeLab.Bridge/ChromeLabProtocol.cs`, `src/OneBrain.ChromeLab.Bridge/ChromeLabOptions.cs`, `src/OneBrain.ChromeLab.Bridge/Stealth/**`, `src/OneBrain.ChromeLab.Bridge/Sessions/**` | Protected scope. No changes made. |
| Stealth core/fingerprinter | READ_ONLY_PROTECTED | `stealth-engine/**`, `stealth-panel/**` | Existing fingerprinting surface is runtime/stealth-oriented. It is not the page capability classifier. No changes made. |
| Chrome Extension legacy surface | READ_ONLY_PROTECTED | `browser-extension/onebrain-chrome-lab/**` | Legacy/no-default runtime surface. Not touched by this block. |
| Perception contracts/classifier/router | CREATE_NEW | `src/OneBrain.BrowserPerception/**` | New fixture-safe read-only perception foundation. |
| Perception tests | CREATE_NEW | `tests/OneBrain.Safety.Tests/CloakBrowserPerceptionRouterFoundationTests.cs` | Pure deterministic tests over fixtures. |

## Fingerprinter Boundary

The existing fingerprinting code is in the stealth/protected runtime area and is treated as browser/stealth fingerprinting, not page capability classification. The new classifier intentionally uses the term `PageCapabilityClassifier` and operates only on `BrowserPerceptionSnapshot` metadata. It does not inspect or change fingerprinting behavior.

## Reuse Recommendation

Reuse the existing CloakBrowser/CDP runtime through interfaces only. Future live collectors should adapt CDP Browser Skills metadata into `BrowserPerceptionSnapshot` without changing protected executor or stealth runtime internals.

## Risks

| Risk | Status | Mitigation |
| --- | --- | --- |
| Confusing page classification with stealth fingerprinting | Controlled | New names avoid `PageFingerprinter`; protected stealth files remain untouched. |
| Accidentally turning perception into automation | Controlled | Router returns strategies only; no productive actions or injection are implemented. |
| Collecting sensitive page payloads | Controlled | Snapshot V0 is metadata-only, fixture-safe, and stores no raw DOM/cookies/storage/input values. |
| Need for live CDP collector in future | Pending | Future block should add a read-only adapter boundary, not change protected executor. |

## Protected Files Not Touched

No diff was introduced under protected post-M1345 paths. The block created a separate perception project and QA/architecture documentation only.
