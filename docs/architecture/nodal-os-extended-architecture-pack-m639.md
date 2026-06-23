# NODAL OS Extended Architecture Pack M639

Decision: `M639 CERRADO / ROADMAP_CONSOLIDATION_UPDATE_READY`

## Scope

This architecture pack consolidates the current architecture after the installed Chrome extension QA/evidence line. It is documentation-only.

It does not change product code, extension JavaScript, manifest, CSP, permissions, bridge source, provider code, runtime code, cloud services, filesystem access or browser automation.

## Current Architecture Snapshot

NODAL OS currently has these active architectural surfaces:

- Chrome extension shell under `browser-extension/onebrain-chrome-lab`.
- Local bridge under `src/OneBrain.ChromeLab.Bridge`.
- Safety and evidence tests under `tests/OneBrain.Safety.Tests`.
- Evidence artifacts under `artifacts/agent-operations`.
- Reports under `docs/reports`.

The current installed-extension line is evidence-ready, not public-release-ready.

## Extension Boundary

Current state:

- Installed extension evidence is clean by user-reported Runtime and service worker DevTools validation.
- CSP evidence indicates no visible CSP violations in the clean manual evidence pass.
- Bridge WebSocket evidence indicates no reconnecting state and no repeated Bridge WebSocket error in the clean manual evidence pass.

Hard boundary:

- No JS changes.
- No manifest changes.
- No CSP changes.
- No permission changes.
- No host permission changes.
- No storage key changes.
- No port/alarm rename.

Future architectural work must be routed through dedicated milestones with tests and evidence.

## Bridge Boundary

Current state:

- Bridge liveness verified on `127.0.0.1:8787`.
- `/health`, `/runtime`, `/debug`, `/config/public` and `/ws/extension` upgrade have PASS evidence.
- Clean manual evidence indicates extension and bridge connect correctly.

Hard boundary:

- No bridge source changes in M639.
- No protocol changes.
- No productive runtime enablement.
- No provider enablement.

Future bridge work must keep startup/liveness scripts and runbooks in sync.

## Runtime Boundary

Current state:

- Runtime status can be observed through extension and bridge evidence.
- Runtime productization remains NO-GO.

Blocked:

- Productive runtime execution.
- Shell/subprocess.
- Filesystem mutation.
- Browser automation execution.
- Connector actions.
- Capability unlock.

Future prerequisites:

- Runtime enablement plan.
- Approval model.
- Evidence ledger model.
- Failure/rollback policy.
- Disabled-by-default release switch.

## Provider Boundary

Current state:

- Provider/cloud remains NO-GO.
- Provider risk remains documented because provider client/path exists but release is not approved.
- API-key-present behavior must remain gated before any public/provider release.

Blocked:

- Provider calls.
- BYOK release.
- Secret storage changes.
- Prompt execution.
- Cloud routing.
- Provider runtime release.

Future prerequisites:

- Provider Runtime Release Gate.
- BYOK consent review.
- Redaction and prompt evidence policy.
- Provider prompt naming cleanup.
- No-secret-in-artifact guarantee.

## Workspace / Filesystem Boundary

Current state:

- No filesystem release is approved.
- Workspace/file access remains planning-only for release purposes.

Blocked:

- File picker release.
- Workspace scan.
- Path jail mutation.
- Export workflows.
- File write/update/delete.

Future prerequisites:

- Path jail plan.
- Workspace consent plan.
- Read-only evidence plan.
- Redaction policy.
- Release rollback plan.

## Approval / Evidence Architecture

Current state:

- Installed extension evidence artifacts are now sufficient to start productization planning.
- Clean manual Runtime and DevTools evidence has been recorded.

Required future consolidation:

- Public release blocker register.
- Release candidate evidence bundle.
- Approval matrix for runtime/provider/filesystem.
- Evidence retention and redaction policy.

Non-goals:

- Approval binding that executes.
- Runtime unlock.
- Provider unlock.
- Filesystem unlock.

## Browser Automation Boundary

Current state:

- Browser automation remains separate from installed extension public release.
- M639 does not activate browser automation.

Blocked:

- Productive browser automation.
- External target automation.
- Connector actions.
- Browser side effects.

Future prerequisites:

- Consent model.
- External target policy.
- No-side-effect audit.
- Evidence model.

## Release Architecture Invariants

- Clean evidence does not equal public release approval.
- Release Evidence Gate GO does not unlock runtime.
- Runtime evidence does not unlock runtime execution.
- DevTools clean evidence does not unlock provider/cloud.
- Bridge liveness does not unlock public release.
- Roadmap consolidation does not unlock product code changes.

## Dependency Map

- M637H feeds M638 with bridge liveness reload QA.
- M638 creates release evidence gate.
- M638A-MANUAL-RETRY closes clean Runtime/DevTools evidence.
- M639 consolidates roadmap and architecture.
- M640-M642 should consolidate public release blockers and future runtime planning.

## Architecture Decisions Consolidated

- Keep extension release evidence line separate from provider/runtime release line.
- Keep bridge local loopback as the current supported evidence target.
- Keep public release blocked until productization readiness review.
- Keep provider/cloud/filesystem blocked until dedicated gates.
- Keep manual evidence explicit and non-inferred.
- Keep raw console excerpts and secrets out of artifacts.

## Recommended Next Architecture Work

M640-M642 should produce:

- Productization readiness review.
- Public release blockers consolidation.
- Future runtime enablement plan.
- Release candidate architecture gate.
