# M662 Host Permissions Narrowing

Decision: `M662 CERRADO / HOST_PERMISSIONS_NARROWING_READY`

## Scope

M662 applies narrowing only to the public manifest variant. The internal manifest baseline remains unchanged.

## Host Permissions

Internal baseline:

- `http://*/*`
- `https://*/*`

Public variant:

- `http://127.0.0.1/*`
- `http://localhost/*`

The public variant removes broad HTTP/HTTPS wildcard host access.

## Content Script Matches

Internal baseline registers `content_script.js` on broad HTTP/HTTPS wildcard origins.

Public variant omits automatic content scripts. This is the most restrictive safe default for a public candidate because it avoids automatic injection on third-party origins.

Future milestones may evaluate optional host permissions, `activeTab`, or user-granted origins before reintroducing site-specific content script behavior.

## Feature Impact

Expected public-build limitations:

- No automatic external-origin content script behavior.
- Site-specific browser-side features require a future user-granted origin design.
- Runtime tab and local bridge UI remain viable through extension pages and local loopback host permissions.
- Provider/cloud/filesystem/browser automation/capability unlock remain disabled.

## Release State

Public Build Candidate: ready for static validation and future manual QA.

Public Release: NO-GO.
