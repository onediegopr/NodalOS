# M652 Packaging Candidate Artifact Prep

Decision: `M652 CERRADO / PACKAGING_CANDIDATE_ARTIFACT_PREP_READY`

## Scope

M652 prepares an internal packaging candidate plan and evidence only.

It does not create a public release, upload to Chrome Web Store, sign with production credentials, modify manifest, change host permissions, change JavaScript, change bridge source, or enable runtime/provider/filesystem/browser automation.

## Package Candidate Scope

Scope: local-first controlled internal evidence candidate.

Allowed package target: internal review only.

Public release: `NO-GO`.

## Candidate Contents

Expected package contents:

- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`
- required static extension assets if present.

Do not include:

- secrets or API keys,
- local build output not required by extension,
- `.git`,
- logs,
- temporary files,
- user data,
- provider credentials,
- NODRIX files.

## Caveats

- Host permissions remain broad and are justified only for internal candidate use.
- Runtime productive execution remains disabled.
- Provider/cloud remains disabled.
- Filesystem and browser automation remain disabled.

## Packaging Method

Recommended method: prepare deterministic internal package in a future packaging milestone, generate checksums, and attach evidence. No package is produced in M652.

## Checksums Plan

Use SHA-256 checksums for all packaged files and for the final internal package artifact if created later.

## Go / No-Go

Internal packaging evidence prep: GO.

Public release packaging: NO-GO.
