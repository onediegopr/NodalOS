# M661 Public Manifest Variant Patch

Decision: `M661 CERRADO / PUBLIC_MANIFEST_VARIANT_PATCH_READY`

## Scope

M661 creates a separate public manifest variant while preserving the installed/internal baseline manifest.

## Internal Baseline

`browser-extension/onebrain-chrome-lab/manifest.json` remains the internal candidate baseline. It preserves the existing broad internal QA permission posture:

- `host_permissions`: `http://*/*`, `https://*/*`
- `content_scripts.matches`: `http://*/*`, `https://*/*`

This baseline remains scoped to controlled internal candidate distribution only.

## Public Variant

`browser-extension/onebrain-chrome-lab/manifest.public.json` was added as a public candidate variant.

Public variant changes:

- Narrows `host_permissions` to `http://127.0.0.1/*` and `http://localhost/*`.
- Omits automatic `content_scripts` registration to avoid broad external origin injection.
- Keeps CSP unchanged from the internal baseline.
- Keeps sidepanel, service worker, content script file, recipe core, and bridge source unchanged.

## Release State

Internal Candidate: GO.

Public Build Candidate: defined.

Public Release: NO-GO.

Chrome Web Store: NO-GO.

Manual QA remains required before any public release decision.
