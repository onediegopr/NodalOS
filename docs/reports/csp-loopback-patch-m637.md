# M637 - CSP Loopback Patch

## Decision

M637 CERRADO / CSP_LOOPBACK_PATCH_READY

## Scope

This milestone applies a manifest-only CSP patch.

Modified product file:

- `browser-extension/onebrain-chrome-lab/manifest.json`

Only changed field:

- `content_security_policy.extension_pages`

## CSP Before

`script-src 'self'; object-src 'self'; connect-src 'self' http://*:* ws://*:*;`

## CSP After

`script-src 'self'; object-src 'self'; connect-src 'self' http://127.0.0.1:* ws://127.0.0.1:* http://localhost:* ws://localhost:*;`

## Boundary Confirmation

Unchanged manifest fields:

- `manifest_version`
- `name`
- `short_name`
- `description`
- `version`
- `permissions`
- `host_permissions`
- `background`
- `side_panel`
- `action`
- `content_scripts`

Unchanged product files:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/content_script.js`
- `browser-extension/onebrain-chrome-lab/recipe_core.js`

## Explicit Non-Changes

- No JS changes.
- No runtime changes.
- No WebSocket logic changes.
- No storage key changes.
- No port or alarm rename.
- No permissions changes.
- No host permissions changes.
- No provider/cloud, filesystem, productive consent, or capability changes.

## Caveat

IPv6 loopback `::1` was not added. The M636 readiness inventory recorded that the code recognizes `::1` for loopback checks, but this patch intentionally follows the M636 candidate: `127.0.0.1` and `localhost` only.

## Go / No-Go

- Manual reload QA after CSP patch: GO and required.
- Public release: NO-GO.
- JS changes: NO-GO.
- Runtime changes: NO-GO.
- Provider/cloud: NO-GO.
- Filesystem: NO-GO.

Recommended next milestone: M637A Manual Reload QA After CSP Loopback Patch.
