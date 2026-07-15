# NODAL OS

NODAL OS is a local-first mission-control foundation for supervised AI work on real projects. The repository currently validates bounded planning, approval, execution, semantic verification, evidence, handoff and teach-by-demonstration flows without granting production or unrestricted execution authority.

## Current status

- Canonical integration branch: `main`.
- Runtime: .NET 11 preview, pinned by `global.json`.
- Product maturity: local/dev technical foundation, not a production release.
- Browser target: CloakBrowser direct CDP; ChromeLab remains lab/transition only.
- Desktop distribution: no signed installer, updater channel or published release yet.
- Cloud: optional by product strategy; no cloud dependency is required by the current local runtime.

## Validated product foundations

- bounded workspace understanding and reviewed plan context;
- mission-level approval with controlled, verifiable actions;
- test-owned file create and exact-hash update with cleanup;
- evidence, timeline projections and deterministic handoff export;
- policy-aware model routing and fallback foundations;
- deterministic non-executing Expert Advisor;
- CognitiveSnapshotV2, SemanticVerifierV2 and Trusted Control Flow;
- verified executable skill memory and localized repair foundations;
- Teach NODAL fixture compiler, bounded capture session and Windows UIA observation adapter;
- local/dev Mission Control and Runtime Inspector surfaces.

## Build and test

Install the SDK version declared in `global.json`, then run:

```powershell
dotnet restore OneBrain.slnx
dotnet build OneBrain.slnx --configuration Release

dotnet test tests/OneBrain.Runtime.Tests/OneBrain.Runtime.Tests.csproj --configuration Release
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --configuration Release
dotnet test tests/OneBrain.ChromeLab.Tests/OneBrain.ChromeLab.Tests.csproj --configuration Release
```

The current repository has no root Node package, Cargo workspace or Tauri application. `npm`, `cargo` and `tauri build` are therefore not applicable to this codebase until the desktop distribution layer is deliberately introduced.

## Repository map

- `src/` — product and runtime projects.
- `tests/` — Runtime, Recipes, Safety and ChromeLab suites.
- `docs/architecture/` — current architecture and compact roadmap.
- `docs/audit/` — technical audits and evidence-backed findings.
- `eng/ci/` — reusable CI smoke scripts.
- `eng/audit/` — deterministic repository inventory tooling.
- `browser-extension/` and `src/OneBrain.ChromeLab.Bridge/` — lab/transition browser surface.
- `apps/nexa-test-owned-target/` — controlled test-owned web target.

## Canonical reading order

1. `docs/architecture/nodal-os-current-mvp-roadmap-compact.md`
2. `docs/architecture/nodal-os-mvp-vertical-slice-canonical.md`
3. `docs/audit/nodal-os-total-technical-audit-2026-07-15.md`
4. `docs/operations/branch-governance.md`
5. `CHANGELOG.md`

## Guardrails

- No hidden fallback to system Chrome, Edge or default Playwright Chromium.
- No raw secrets in logs, evidence or handoffs.
- No observed UI text may rewrite the operator's mission.
- No sensitive controlled action without the required mission/scope authorization.
- No success promotion without verification and evidence.
- No public, production, billing or commercial authority is implied by local/dev fixtures.
