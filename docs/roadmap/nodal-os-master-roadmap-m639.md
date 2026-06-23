# NODAL OS Master Roadmap M639

Decision: `M639 CERRADO / ROADMAP_CONSOLIDATION_UPDATE_READY`

## Scope

M639 consolidates the current roadmap after the installed Chrome extension evidence line. It is documentation and planning only.

M639 does not enable runtime execution, provider/cloud, filesystem, new permissions, CSP changes, manifest changes, bridge changes, JavaScript changes, browser automation, or public release.

## Current State

NODAL OS has a clean installed-extension evidence line through M638A-MANUAL-RETRY:

- M637H: `BRIDGE_LIVENESS_RELOAD_QA_PASSED`
- M638: `INSTALLED_EXTENSION_RELEASE_EVIDENCE_GATE_READY`
- M638A-MANUAL-RETRY: `CLEAN_RUNTIME_DEVTOOLS_EVIDENCE_READY`

Validated evidence:

- Runtime OK.
- Bridge OK.
- DevTools OK by user-reported manual evidence.
- CSP violations: no.
- `ERR_CONNECTION_REFUSED`: no.
- `invalid_token`: no.
- close `1008`: no.
- Bridge WebSocket error: no.
- WebSocket reconnecting: no.

Current gates:

- Release Evidence Gate: GO.
- Public Release: NO-GO.
- JS changes: NO-GO.
- Runtime productization: NO-GO.
- Provider/cloud/filesystem: NO-GO.

## Roadmap Principles

- Evidence-first: release decisions require artifacts, reports, tests or explicit manual evidence.
- Policy-first: runtime, provider, filesystem and automation remain blocked until explicit gates are closed.
- Local-first: bridge and extension remain loopback/local-only for the current release evidence line.
- Approval-first: sensitive execution requires future approval gates and cannot be inferred from clean QA.
- Redaction-first: raw secrets, raw console dumps and private payloads must not be stored in evidence artifacts.
- No silent unlocks: documentation, clean DevTools evidence and roadmap clarity do not unlock runtime or public release.

## Phase 0: Installed Extension Evidence Closeout

Status: complete for evidence gate, not complete for public release.

Closed:

- Installed extension visible QA line.
- NODAL OS visible naming evidence line.
- CSP loopback tightening and QA follow-up.
- Bridge liveness/reconnect diagnosis and fix line.
- Clean manual Runtime + service worker DevTools evidence.

Remaining:

- Public release blocker review.
- Store/package readiness review.
- Host permissions justification.
- Provider/runtime release gate.
- Final public release go/no-go.

## Phase 1: Productization Readiness Planning

Expected block range: M640-M642.

Purpose:

- Consolidate public release blockers.
- Review package/store readiness.
- Define future runtime enablement plan without enabling runtime.

Deliverables:

- Productization readiness review.
- Public release blockers consolidation.
- Future runtime enablement plan.
- Updated public release go/no-go register.

NO-GO in this phase:

- No public release.
- No provider enablement.
- No runtime execution.
- No filesystem activation.
- No permission/CSP changes without separate patch milestone.

## Phase 2: Release Candidate Evidence Pack

Purpose:

- Produce a release-candidate evidence bundle for installed extension review.
- Keep release public NO-GO until all blockers are closed.

Required evidence:

- Clean Runtime tab screenshot or equivalent manual report.
- Clean service worker DevTools evidence.
- Host permissions review and justification.
- CSP loopback rationale.
- Provider/runtime disabled-state proof.
- Naming/microcopy proof.
- Bridge startup/liveness runbook verification.

## Phase 3: Runtime Enablement Planning

Purpose:

- Design the future path from disabled runtime to preview runtime without enabling it.

Required gates:

- Execution authorization gate.
- Approval model review.
- Evidence model integration.
- Runtime state machine review.
- No external side effects proof.
- Rollback/disable plan.

NO-GO:

- No productive runtime.
- No task execution.
- No shell/subprocess.
- No filesystem mutation.
- No browser automation execution.

## Phase 4: Provider/BYOK Planning

Purpose:

- Define provider readiness and BYOK consent requirements before any provider release.

Required gates:

- Provider consent UX.
- Secret storage policy.
- Redaction policy.
- Prompt/evidence logging policy.
- Provider disabled-state proof.
- Naming debt cleanup for provider prompt surfaces.

NO-GO:

- No provider/cloud release.
- No API key handling changes.
- No external provider calls.
- No prompt execution unlock.

## Phase 5: Workspace/FileSystem Planning

Purpose:

- Plan a safe local workspace model before any real file access or mutation.

Required gates:

- Path jail policy.
- Read-only workspace proof.
- User consent model.
- Evidence and redaction model.
- No mutation proof.

NO-GO:

- No file picker activation.
- No workspace scan activation.
- No filesystem write/update/delete.
- No export workflow.

## Phase 6: Browser Automation Planning

Purpose:

- Keep browser automation research and release lines separated from installed extension public release.

Required gates:

- Browser automation capability review.
- Consent gate.
- External target policy.
- Evidence model.
- No productive execution proof.

NO-GO:

- No browser automation capability unlock.
- No connector actions.
- No external side effects.

## Release Blocker Register Summary

Critical:

- Public release remains blocked until productization readiness review closes.
- Provider/runtime must remain disabled for public release unless a dedicated release gate approves it.

Medium:

- Host permissions require final release justification.
- IPv6 loopback remains a documented caveat because default evidence uses `127.0.0.1`.
- Provider prompt legacy naming debt remains documented.

Low:

- Some historical test warnings remain unrelated to installed extension release readiness.

## Percentages

- Installed Extension Evidence Gate readiness: 95%.
- Public Release readiness: 40%.
- Runtime productization readiness: 20%.
- Provider/cloud readiness: 15%.
- Filesystem/workspace readiness: 20%.
- Browser automation release readiness: 15%.
- Roadmap/architecture clarity: 85%.

These percentages are planning indicators, not release permissions.

## Recommended Next Block

M640-M642:

- Productization Readiness Review.
- Public Release Blockers Consolidation.
- Future Runtime Enablement Plan.

Constraint: planning only, no runtime enablement.
