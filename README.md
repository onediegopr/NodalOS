# NODAL OS

NODAL OS is a local-first AI Mission Control for supervised work on real projects. The current private-beta path covers workspace selection, mission planning, mission-scope approval, one bounded reversible file action, deterministic verification, evidence/timeline, secure BYOK routing and Windows packaging without granting unrestricted or production authority.

## Current status

- Canonical integration branch: `main`.
- Runtime: .NET 11 preview, pinned by `global.json`.
- Product maturity: installable Windows private beta; not a public or production release.
- Desktop distribution: test-signed self-contained `win-x64` MSIX validated through clean build, install, launch, health checks and uninstall.
- Public distribution: blocked until repository/product terms, production signing identity and release/update channel are selected.
- Browser target: CloakBrowser direct CDP; ChromeLab remains an explicit lab/transition surface and is excluded from the packaged product route set.
- License: not declared yet; external/customer/commercial distribution remains blocked until terms are selected.
- Cloud: optional by product strategy; no Vercel or Neon deployment is required by the current local runtime.

## Validated private-beta loop

1. Start from a clean Mission Control state with no synthetic mission, model, fallback or evidence.
2. Select and persist a protected local workspace.
3. Create a real mission and review its plan.
4. Approve one exact mission-scoped action over `NODAL_HANDOFF.md`.
5. Execute a create or exact-hash update atomically.
6. Verify exact bytes and SHA-256, retain evidence and offer a guarded rollback.
7. Configure a BYOK or loopback OpenAI-compatible model route through opaque DPAPI-backed secret references.
8. Continue automatically through an already authorized fallback when privacy, capability and budget remain compatible.
9. Resume or recover from stale, changed or failed-closed execution states through the same Mission Control surface.

## Additional validated foundations

- bounded workspace understanding and reviewed planning context;
- canonical evidence and timeline projections;
- deterministic non-executing Expert Advisor foundation;
- CognitiveSnapshotV2, SemanticVerifierV2 and Trusted Control Flow;
- verified executable skill memory and localized repair foundations;
- Teach NODAL fixture compiler, bounded capture session and Windows UIA observation adapter;
- local/dev Runtime Inspector and explicit legacy lab routes.

## Build and test

Install the SDK version declared in `global.json`, then run:

```powershell
dotnet restore OneBrain.slnx
dotnet build OneBrain.slnx --configuration Release

dotnet test tests/OneBrain.Runtime.Tests/OneBrain.Runtime.Tests.csproj --configuration Release
dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --configuration Release
dotnet test tests/OneBrain.ChromeLab.Tests/OneBrain.ChromeLab.Tests.csproj --configuration Release
```

The repository intentionally has no root Node package, Cargo workspace or Tauri application. Windows distribution uses the existing .NET application and native MSIX tooling; `npm`, `cargo` and `tauri build` are not applicable to this product path.

## Repository map

- `src/` — product and runtime projects.
- `tests/` — Runtime, Recipes, Safety and ChromeLab suites.
- `docs/architecture/` — current architecture and compact roadmap.
- `docs/audit/` — technical audits and evidence-backed findings.
- `eng/ci/` — reusable CI and process-smoke scripts.
- `eng/audit/` — deterministic repository inventory tooling.
- `eng/packaging/` — native MSIX packaging pipeline.
- `browser-extension/` and `src/OneBrain.ChromeLab.Bridge/` — lab/transition browser surface.
- `apps/nexa-test-owned-target/` — controlled test-owned web target.

## Canonical reading order

1. `docs/architecture/nodal-os-current-mvp-roadmap-compact.md`
2. `docs/architecture/nodal-os-mvp-vertical-slice-canonical.md`
3. `docs/audit/nodal-os-total-technical-audit-2026-07-16.md`
4. `docs/operations/branch-governance.md`
5. `CHANGELOG.md`

## Guardrails

- No hidden fallback to system Chrome, Edge or default Playwright Chromium.
- No raw secrets, absolute workspace paths or provider response content in logs, evidence or handoffs.
- No observed UI text may rewrite the operator's mission.
- No sensitive controlled action without the required mission/scope authorization.
- No success promotion without verification and evidence.
- No automatic rollback after the verified result has changed.
- No lab/demo routes in the packaged private-beta product surface.
- No public, production, billing or commercial authority is implied by private-beta validation.
