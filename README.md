# NODAL OS

NODAL OS is a local-first AI Mission Control for supervised work on real projects. The current private-beta path covers workspace selection, mission planning, mission-scope approval, one bounded reversible file action, deterministic verification, evidence/timeline, secure BYOK routing, review-only semantic workflow teaching and Windows packaging without granting unrestricted or production authority.

## Current status

- Canonical integration branch: `main`.
- Runtime: .NET 10 LTS, with SDK `10.0.302` pinned by `global.json` and prerelease SDKs disabled.
- Product maturity: installable Windows private beta; not a public or production release.
- Desktop distribution: test-signed self-contained `win-x64` MSIX `0.1.0.4` validated through clean build, exact package-derived third-party inventory, install, packaged core loop, guarded rollback, uninstall and exact test-certificate cleanup.
- Teach NODAL: `/teach` exposes an application-scoped semantic Record → Review Draft flow. It stores no video, audio, raw input, coordinates, screenshots or DOM; drafts remain local, editable, review-only and non-executable.
- Field validation: design-partner runbook ready; no external partner sessions completed yet.
- Public distribution: blocked until repository/product terms, review of the generated notices, production signing identity and real release/update channel are selected.
- Browser target: CloakBrowser direct CDP; ChromeLab remains an explicit lab/transition surface and is excluded from the packaged product route set.
- License: not declared yet; generated third-party notice files are technical payload evidence, not adopted NODAL OS terms or distribution authority.
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
10. Download a human-readable handoff derived from the canonical mission, timeline and evidence.

## Teach NODAL review-only slice

The `/teach` product surface reuses the existing Living Skills foundations instead of introducing another recorder or runtime:

1. name the workflow and select one application by switching it to foreground;
2. record one semantic step at a time through bounded UI Automation before/after snapshots;
3. enter or dictate the trusted intent with Windows dictation (`Win + H`);
4. bind typed values only through `variable-ref:`, `literal-ref:`, `secret-ref:` or `secret://` references;
5. finish the demonstration and inspect the generated proposal;
6. edit title, summary, step intent and visible targets;
7. save a local versioned draft or discard it.

Saving a draft does not enable replay, scripts or execution authority. A matching application profile and normalized workflow title is proposed as an update candidate rather than silently creating a duplicate. Global hooks, stored video/audio, raw keyboard values, pointer coordinates, screenshots, DOM capture, cloud sync and marketplace behavior remain out of scope.

## Additional validated foundations

- package-derived `ThirdParty` inventory matching the installed dependency manifest, with exact source notice files and hashes while legal/public-distribution approval remains false;
- opt-in redacted startup/error/process diagnostics and local startup, first-value and mission-completion timings, stored with bounded retention and no upload path;
- bounded workspace understanding and reviewed planning context;
- canonical evidence and timeline projections;
- deterministic non-executing Expert Advisor foundation;
- CognitiveSnapshotV2, SemanticVerifierV2 and Trusted Control Flow;
- verified executable skill memory and localized repair foundations;
- Teach NODAL compiler, bounded capture session, Windows UIA observation adapter and review-only product draft surface;
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
- `docs/private-beta/` — design-partner field-validation operations and redacted session template.
- `docs/distribution/` — package, signing, license-status and release boundaries.
- `eng/ci/` — reusable CI and process-smoke scripts.
- `eng/audit/` — deterministic repository inventory tooling.
- `eng/packaging/` — native MSIX packaging pipeline.
- `eng/release/` — deterministic package-derived third-party notice generation.
- `browser-extension/` and `src/OneBrain.ChromeLab.Bridge/` — lab/transition browser surface.
- `apps/nexa-test-owned-target/` — controlled test-owned web target.

## Canonical reading order

1. `docs/architecture/nodal-os-current-mvp-roadmap-compact.md`
2. `docs/architecture/nodal-os-mvp-vertical-slice-canonical.md`
3. `docs/private-beta/design-partner-field-validation.md`
4. `docs/audit/nodal-os-total-technical-audit-2026-07-16.md`
5. `docs/operations/branch-governance.md`
6. `CHANGELOG.md`

## Guardrails

- No hidden fallback to system Chrome, Edge or default Playwright Chromium.
- No raw secrets, absolute workspace paths or provider response content in logs, evidence or handoffs.
- No observed UI text may rewrite the operator's mission.
- No sensitive controlled action without the required mission/scope authorization.
- No success promotion without verification and evidence.
- No automatic rollback after the verified result has changed.
- No lab/demo routes in the packaged private-beta product surface.
- No Teach NODAL draft grants replay, script, execution or product authority.
- No generated third-party inventory or technical signing test grants NODAL OS product, legal, public-distribution or commercial authority.