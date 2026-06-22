# M636 - CSP Tightening Readiness / Loopback Candidate

## Decision

M636 CERRADO / CSP_TIGHTENING_READINESS_READY

## Scope

This milestone is audit-first and readiness-only. No product files were modified.

Inspected files:

- `browser-extension/onebrain-chrome-lab/manifest.json`
- `browser-extension/onebrain-chrome-lab/service_worker.js`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`

## Current CSP

Current extension pages CSP:

`script-src 'self'; object-src 'self'; connect-src 'self' http://*:* ws://*:*;`

Current `connect-src` is broader than needed for the local bridge because it allows arbitrary HTTP and WebSocket hosts.

## Required Endpoints

Default bridge config:

- Host: `127.0.0.1`
- Port: `8787`

Observed required HTTP loopback endpoints:

- `http://127.0.0.1:8787/config/public`
- `http://127.0.0.1:8787/health`
- `http://127.0.0.1:8787/runtime`
- `http://127.0.0.1:8787/clients`
- `http://127.0.0.1:8787/last-events`
- `http://127.0.0.1:8787/debug`
- `http://127.0.0.1:8787/api/runs`
- `http://127.0.0.1:8787/pairing/local-token`

Observed required WebSocket loopback endpoint:

- `ws://127.0.0.1:8787/ws/extension`

The UI and service worker allow host/port configuration. The current default is `127.0.0.1:8787`; `localhost` is a reasonable loopback variant for the candidate. The code also treats `::1` as loopback for local pairing, but this readiness milestone does not include an IPv6 CSP patch decision.

## External Connectivity

No product requirement was found for:

- Remote domains.
- External HTTPS APIs.
- CDN scripts.
- Provider/cloud APIs.
- Arbitrary remote WebSocket hosts.

## Candidate CSP

Candidate `connect-src`:

`'self' http://127.0.0.1:* ws://127.0.0.1:* http://localhost:* ws://localhost:*`

Candidate full extension pages CSP:

`script-src 'self'; object-src 'self'; connect-src 'self' http://127.0.0.1:* ws://127.0.0.1:* http://localhost:* ws://localhost:*;`

## Patch Decision

No manifest patch was applied in M636.

Reason:

- M636 is readiness-only by design.
- A CSP change can break bridge connectivity if the host variant or CSP syntax is incomplete.
- The next milestone should apply the manifest-only patch and then require manual reload QA.

## Explicit Non-Changes

- No manifest changes.
- No permissions changes.
- No host permissions changes.
- No JS changes.
- No runtime changes.
- No WebSocket logic changes.
- No storage key changes.
- No port or alarm rename.
- No provider/cloud, filesystem, productive consent, or capability changes.

## Go / No-Go

- CSP tightening patch: GO for a dedicated next milestone.
- Public release: NO-GO.
- JS changes: NO-GO.
- Runtime changes: NO-GO.
- Provider/cloud: NO-GO.
- Filesystem: NO-GO.

Recommended next milestone: M637 CSP Loopback Patch.
