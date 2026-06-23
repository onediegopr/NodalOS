# M705 Public Package Candidate Freeze Prep

## Decision

M705 prepares the public package freeze as conditional, not final.

## Staging Audit

The public staging folder is the controlled package candidate source:

`artifacts/manual-qa/public-variant-staging`

Expected files:

- `manifest.json`
- `sidepanel.html`
- `sidepanel.css`
- `sidepanel.js`
- `service_worker.js`
- `content_script.js`
- `recipe_core.js`
- `README.md`

The staging manifest materializes the public manifest, uses localhost and 127.0.0.1 host permissions, and does not include broad host wildcards or automatic external content scripts.

## Exclusions

The candidate must exclude local config, tokens, API keys, logs, temporary profiles, user data, screenshots with secrets, and long raw logs.

## Checksum Plan

SHA-256 checksums are planned for a future explicit freeze gate. No final signed ZIP is created in this milestone.

## Freeze State

Freeze prep is ready. Public package freeze remains NO-GO because evidence, screenshots, and privacy/support URLs remain pending.
