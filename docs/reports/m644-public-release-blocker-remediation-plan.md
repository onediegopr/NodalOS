# M644 Public Release Blocker Remediation Plan

Decision: `M644 CERRADO / PUBLIC_RELEASE_BLOCKER_REMEDIATION_PLAN_READY`

## Scope

M644 is planning and release-blocker remediation planning only.

It does not modify manifest, host permissions, JavaScript, bridge source, CSP, permissions, runtime, provider/cloud, filesystem, or public release state.

## Current Release Position

- Release Candidate Evidence Pack: ready from M643.
- Release Evidence Gate: GO.
- Public Release: NO-GO.
- Runtime: NO-GO.
- Provider/cloud: NO-GO.
- Filesystem: NO-GO.

## Host Permissions Remediation Plan

### Current State

Host permissions remain broad and do not yet have final public release justification.

### Risk

Broad host permissions increase:

- Chrome Web Store review surface.
- User trust concerns.
- Privacy disclosure burden.
- Support burden if users assume broad access is active.

### Remediation Options

Option A: retain broad host permissions with formal justification.

- Impact: lowest implementation risk.
- Cost: higher store/privacy review burden.
- Required evidence: permission inventory and user-facing disclosure.

Option B: narrow host permissions before public release.

- Impact: may reduce review/user-trust risk.
- Cost: requires manifest patch milestone and full installed extension reload QA.
- Required evidence: regression QA, CSP/permission tests, sidepanel/runtime verification.

Option C: split public release into local-only limited edition and future expanded edition.

- Impact: clearer release story.
- Cost: release packaging and roadmap complexity.
- Required evidence: public edition scope and future permission expansion gate.

### Recommended Option

Recommended: Option C for release planning, with Option B as the technical remediation path if public release requires permission reduction.

Do not change host permissions inside M644.

### Closure Criteria

- Host permission inventory complete.
- Release scope decision recorded.
- Privacy/support language prepared.
- If permissions change, dedicated manifest patch milestone and full installed extension QA completed.

## Packaging / Signing / Store Review Plan

Required work:

- Package reproducibility review.
- Extension version candidate identification.
- Signing/publication path decision.
- Chrome Web Store or internal distribution decision.
- Store metadata draft.
- Privacy disclosure.
- Support URL.
- Release notes.
- Rollback plan.
- Final release candidate checklist.

Current status: incomplete.

Public release remains NO-GO until this plan is executed and evidence is recorded.

## Provider / Runtime Public Release Gate Plan

### Current State

- `OpenAiAgentClient` is present in bridge source.
- Provider/cloud remains disabled for release.
- Runtime remains disabled for public release.
- Provider prompt naming debt remains open for provider release.

### Public Release Gate Position

Provider/runtime paths do not block the Release Evidence Gate because the installed extension evidence line is clean and provider/runtime remain disabled.

Provider/runtime paths do block public release unless the release explicitly proves:

- provider/cloud disabled for public release,
- runtime productive execution disabled,
- provider prompt naming not user-visible in the public release path,
- no API key or provider call path is enabled by default,
- no runtime/capability unlock is shipped.

### Closure Criteria

- Disabled-state proof refreshed for the final release candidate.
- Provider prompt naming debt reviewed.
- Provider/BYOK release gate remains future-only or is explicitly closed in a dedicated milestone.
- Public release notes and privacy disclosure state provider/cloud disabled status.

## Public Release Final Closure

M644 closes:

- Remediation plan creation.
- Blocker categorization.
- Recommended remediation paths.
- Next milestone definition.

M644 does not close:

- Host permissions formal justification.
- Packaging/signing/store review.
- Provider/runtime final disabled-state release proof.
- CORS LAN public release posture.
- Microcopy final review.
- Public release approval.

## Go / No-Go

- Release Evidence Gate: GO.
- Public Release: NO-GO.
- Runtime: NO-GO.
- Provider/cloud: NO-GO.
- Filesystem: NO-GO.

## Recommended Next Milestone

M645 Host Permissions Justification + Packaging Store Evidence Prep.
