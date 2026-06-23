# M666 Public Variant Manual Extension QA Contract

Decision: `M666 CERRADO / PUBLIC_VARIANT_MANUAL_EXTENSION_QA_CONTRACT_READY`

## Scope

M666 defines the manual QA contract for the public variant. It does not execute manual QA, does not load Chrome, does not change product files, and does not publish.

## Required Manual QA

Manual QA must be performed against a package/staging directory that uses `manifest.public.json` as the effective package `manifest.json`.

Required checks:

- Chrome extension reload.
- Confirm public variant loaded, not internal baseline.
- Confirm visible name/metadata.
- Runtime tab.
- Service Worker DevTools.
- CSP violations: none expected.
- `ERR_CONNECTION_REFUSED`: none if bridge is alive.
- `invalid_token`: none.
- close 1008/policy violation: none.
- Bridge WebSocket error: none.
- WebSocket reconnect storm: none.
- Critical console errors: none.
- Allowed local origins.
- Disallowed external origins.
- Known limitations visible or documented.

## Evidence Redaction

Evidence must not include secrets, raw API keys, long raw logs, private user data, or unredacted sensitive paths.

## Release State

Manual QA is required before public release. Public release remains NO-GO.
