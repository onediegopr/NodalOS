# M665 Public Package Candidate Prep

Decision: `M665 CERRADO / PUBLIC_PACKAGE_CANDIDATE_PREP_READY`

## Scope

M665 prepares a public package candidate process. It does not create a final signed package, does not publish, does not upload to Chrome Web Store, and does not replace the internal baseline manifest.

## Manifest Selection

The public package candidate must use `browser-extension/onebrain-chrome-lab/manifest.public.json` as the package manifest.

The internal manifest `browser-extension/onebrain-chrome-lab/manifest.json` remains the installed/internal candidate baseline and must not be overwritten.

## Verification Rules

Before any package candidate is accepted:

- Package manifest must be the public manifest.
- Package manifest must not contain `http://*/*` or `https://*/*`.
- Package manifest must not contain content script wildcard matches.
- Package must not contain secrets, logs, temp files, user data, or unreviewed artifacts.

## Runbook Summary

1. Start with a clean worktree.
2. Copy extension files to a temporary candidate staging directory.
3. Replace the staged package manifest with `manifest.public.json` as `manifest.json` inside the staging directory.
4. Validate staged manifest JSON.
5. Validate no wildcard host permissions or content script matches.
6. Validate package contents and exclusions.
7. Capture evidence.
8. Do not publish or sign as final release.

## Release State

Public Package Candidate: prepared.

Public Release: NO-GO.

Chrome Web Store: NO-GO.
