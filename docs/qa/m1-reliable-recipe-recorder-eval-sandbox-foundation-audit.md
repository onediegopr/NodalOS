# M1 Reliable Recipe / Recorder / Eval / Sandbox Foundation Audit

## Guard

- Repo path: `C:\DESARROLLO\NodalOS\Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- HEAD initial: `769281f1b356fa38066dc9cd586ab48b63597b4b`
- Remote: `https://github.com/onediegopr/NodalOS.git`
- Origin sync: `0/0`
- Initial worktree: clean
- Repo identity: NODAL OS confirmed.
- NODRIX: not the active repo.

## Files and Areas Reviewed

- `src/OneBrain.Core/Recipes/*`
- `src/OneBrain.BrowserPerception/*`
- `tests/OneBrain.Recipes.Tests/*`
- `docs/architecture/*`
- `docs/recipes/*`
- `docs/qa/*`
- `docs/handoff/*`

## Existing Modules Found

- Recipe Runtime foundation and workitems.
- Recipe policy preflight, limits, validation, risk and deterministic action resolution.
- Evidence pack and timeline projection contracts.
- Human intervention and approval narrative contracts.
- Tool trust registry, secrets by reference and credentialed action gates.
- Trigger/detector observe-only contracts.
- Recipe Lab and Locator Repair Studio contracts.
- Global and LATAM template catalog contracts with composite readiness.
- Capture Draft contracts.
- Read-only Recipe Product Surface, navigation and messaging line.
- BrowserPerception fixture-safe foundation, locator, blockage, evidence redaction, verification and controlled action executor areas.
- Windows computer-use architecture docs and fixture-safe/read-only gate docs.

## Protected Scope

The following areas are protected for this block and were not modified:

- Browser runtime execution, CDP runtime entrypoints and Playwright-backed execution code.
- Cloak/Camoufox/browser profile, runner, remote-control, proxy, challenge and Docker code.
- Desktop automation hooks, UIA live eventing, OS hook/listener, scheduler/worker runtime code.
- Connector/API/network/vault/secret execution surfaces.
- Recorder/replay/real capture code.

## Gaps Found

- The existing Recipe Runtime line is strong for product-ready fixture-safe recipes, but the repo did not have a single clean-room M1 contract surface that names the forward automation architecture across reliable recipes, recorder drafts, eval harness, sandbox readiness and formal perception stack.
- Existing capture contracts cover recipe capture draft, but no OpenAdapt-style recorded interaction trajectory contract existed as a standalone foundation.
- Existing template/composite readiness covers template safety, but no fixture-only eval harness contract existed for deterministic scenario scoring.
- Existing BrowserPerception contracts exist, but no recipe-facing multi-signal perception snapshot contract existed that formally positions OCR as one signal among DOM/accessibility/visual/Set-of-Marks.
- Existing computer-use docs are extensive, but recipe-facing sandbox readiness contracts were not centralized.

## Placement Decision

The M1 contracts belong in `src/OneBrain.Core/Recipes` as a design-only, clean-room Reliable Recipe foundation slice. This avoids creating a duplicate runtime project and keeps the contracts near the existing Recipe Runtime, Product Surface and Capture Draft contracts.

New contracts use `ReliableRecipe*` names where existing `Recipe*` names already exist. This avoids type collisions and makes the M1 foundation explicit as a future-facing design layer, not a replacement for the closed Recipe Runtime line.

## GO/NO-GO

Decision to write contracts: GO.

Reason: the repo guard passed, the working tree was clean, existing architecture supports a contract-only extension, and no protected scope changes are required.

## Initial and Target Percentages

| Area | Before | After Target |
| --- | ---: | ---: |
| Audit/placement | 25% | 100% |
| Reliable Recipe contracts | 15% | 50% |
| Validation/policy gates | 15% | 40% |
| Evidence/timeline recipe linkage | 20% | 30% |
| Recorder draft readiness | 5% | 25% |
| Eval harness readiness | 5% | 25% |
| Sandbox readiness | 5% | 25% |
| Perception stack formalization | 30% | 40% |
| Runtime real autonomy | 0% | 0% intentionally |

## Safety Boundary

This M1 block is contract-only and fixture-safe. It does not add browser runtime execution, CDP calls for runtime use, Playwright runtime use, Cloak state mutation, desktop hooks, real recorder, real sandbox, real screenshot capture, provider/LLM calls, secrets, challenge circumvention, payment, publish, send, delete or productive side effects.
