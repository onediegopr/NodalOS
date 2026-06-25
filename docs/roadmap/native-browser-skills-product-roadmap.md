# Native Browser Skills Product Roadmap

## Decision

- BrowserAct runtime: no integrado.
- BrowserAct dependency: no agregada.
- No BrowserAct dependency in product projects.
- BrowserAct: reference only.
- NODAL OS Native Browser Skills: foundation minima.
- Stealth / Proxy / Captcha: future descriptors, no runtime.

## Why

M1221 showed useful provider patterns: indexed page state, session descriptors, friction handling, recovery notes, network evidence candidates and CDP operation candidates.

NODAL OS should keep those ideas natively without importing BrowserAct or changing the product runtime.

## Current Foundation

- `BrowserSkillManifest`
- `BrowserSkillCapabilityEnvelope`
- `BrowserStateSnapshot`
- `BrowserIndexedElement`
- `BrowserSkillSessionDescriptor`
- `BrowserSessionResilienceReport`
- `AccessFrictionEvent`
- `BlockedFlowRecoveryPlan`
- `HumanTakeoverRequest`
- `NetworkEvidenceCandidate`
- `CdpOperationCandidate`
- `StealthProfile`
- `ProxyRouteProfile`
- `CaptchaChallengeEvent`
- `CaptchaHandlingStrategy`

## Product Surface

Mission Control remains focused on the demo.

Modo avanzado shows a small Browser Skills card:

- Native browser skills: planned
- CDP skills: planned
- BrowserAct: reference only
- Stealth / Proxy / Captcha: future descriptors
- Runtime: not active

## Next Product Step

Return to:

`M1269-M1280 - Installed Sidepanel Manual Verification + Final Demo Polish`

Do not continue Browser Skills until it directly supports the visible product demo.
