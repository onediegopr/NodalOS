# M641 Public Release Blockers Consolidation

Decision: `M641 CERRADO / PUBLIC_RELEASE_BLOCKERS_CONSOLIDATED`

## Scope

M641 consolidates public release blockers only. It does not change manifest, permissions, CSP, JS, bridge, runtime, provider/cloud, filesystem or publication state.

## Public Release Status

Public release remains `NO-GO`.

Release evidence gate remains `GO`.

## Consolidated Blockers

### Host Permissions

Status: open.

Risk: broad host permissions can expand review surface and user trust requirements.

Required before public release:

- Permission inventory.
- Justification for every host permission.
- User-facing privacy/support language.
- Store review readiness.

### Provider / Runtime Gate

Status: open.

Risk: provider client/runtime paths must not be interpreted as release-ready because installed-extension evidence is clean.

Required before provider/runtime release:

- Provider runtime release gate.
- BYOK consent review.
- Redaction and prompt evidence policy.
- Disabled-state proof for public release if provider remains blocked.

### IPv6 Loopback Caveat

Status: documented.

Risk: current evidence line targets `127.0.0.1`; IPv6 `::1` is not the supported evidence target.

Required before public release:

- Decide whether this is an accepted caveat or a future CSP patch milestone.
- Keep default support documentation on `127.0.0.1`.

### CORS LAN Caveat

Status: open/documented.

Risk: LAN candidate output exists in bridge logs and may require explicit public release posture.

Required before public release:

- Confirm LAN access posture.
- Document whether LAN is unsupported, blocked, optional, or future gated.
- Ensure no public release messaging implies LAN support unless explicitly approved.

### Provider Prompt Naming Debt

Status: open.

Risk: legacy prompt naming may reference older naming and confuse release messaging.

Required before provider release:

- Provider prompt surface inventory.
- Naming cleanup plan.
- Evidence that provider prompt naming is not public-release visible when provider is disabled.

### Microcopy Mixed Surface

Status: low-medium/open.

Risk: minor mixed historical microcopy can create brand inconsistency.

Required before public release:

- Final visible copy review.
- Store/listing naming review.
- No NEXA/ONE BRAIN visible primary naming on public release surfaces.

### Release Evidence Requirements

Status: partially met.

Completed:

- Clean Runtime evidence.
- Clean service worker DevTools evidence.
- Bridge liveness PASS.

Required before public release:

- Final release candidate evidence pack.
- Packaging/signing evidence.
- Store review evidence.
- Rollback/support evidence.

### Packaging / Signing / Store Publication

Status: open.

Risk: installed local QA is not equivalent to packaged release readiness.

Required before public release:

- Package reproducibility check.
- Signing/publishing checklist.
- Store metadata review.
- Privacy/support disclosures.
- Final release blocker closure.

## Public Release No-Go Rule

Public release remains blocked until every critical blocker is closed and medium blockers are either closed or explicitly accepted with documented risk.

## Recommended Outcome

Keep public release `NO-GO`. Proceed with M642 future runtime enablement plan and then a dedicated release-candidate blocker closure gate.
