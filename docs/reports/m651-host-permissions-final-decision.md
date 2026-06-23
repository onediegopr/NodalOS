# M651 Host Permissions Final Decision / Manifest Strategy Gate

Decision: `M651 CERRADO / HOST_PERMISSIONS_FINAL_DECISION_READY`

## Scope

M651 is decision, planning, and manifest strategy only.

It does not modify `manifest.json`, host permissions, permissions, JavaScript, bridge source, CSP, runtime, provider/cloud, filesystem, packaging, signing, or public release state.

## Current Host Permissions State

Current `host_permissions`:

- `http://*/*`
- `https://*/*`

Current `content_scripts.matches`:

- `http://*/*`
- `https://*/*`

State inherited from M645:

- `justified_for_internal_candidate`
- `open_for_public_release`

## Strategy Options Reviewed

### 1. Keep Broad Permissions With Strong Justification

Benefit: lowest implementation cost and preserves current installed extension behavior.

Risk: highest Chrome Web Store and user-trust burden.

Public release posture: not recommended as default without explicit founder/store/legal approval.

### 2. Future Narrowing Patch

Benefit: lowers public release risk and improves store review posture.

Risk: requires manifest change, full installed extension reload QA, and regression evidence.

Public release posture: recommended if the public build must avoid broad host permissions.

### 3. Split Internal/Public Build

Benefit: preserves internal/local-first evidence candidate while creating a safer public release path.

Risk: requires build strategy discipline and possibly a future manifest patch.

Public release posture: recommended strategy.

## Recommended Strategy

Recommended strategy: `split_internal_public_build`.

This keeps the current installed extension line valid for internal/local-first evidence use while requiring a dedicated public build/manifest strategy gate before any public release.

## Impact

Internal candidate: GO.

Public release: NO-GO.

Chrome Web Store: blocked until public build permission strategy, privacy/support disclosure, and packaging/store evidence are final.

## Closure Criteria

To close the public release permission blocker, a future milestone must decide and execute one of:

- public build retains broad permissions with final approval and strong disclosure,
- public build narrows host permissions and completes installed extension QA,
- internal and public builds remain separate with explicit package/store evidence.

## Recommended Next Milestone

`M651A Host Permissions Narrowing Patch Plan`

If the team chooses to package only an internal candidate first, `M652 Packaging Candidate Artifact Prep` can proceed separately without public release approval.
