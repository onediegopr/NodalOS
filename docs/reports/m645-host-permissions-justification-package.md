# M645 Host Permissions Justification Package

Decision: `M645 CERRADO / HOST_PERMISSIONS_JUSTIFICATION_PACKAGE_READY`

## Scope

M645 is release-blocker evidence and documentation only.

It does not modify `manifest.json`, `host_permissions`, JavaScript, CSP, permissions, bridge source, runtime, provider/cloud, filesystem, or public release state.

## Current Host Permissions State

The installed Chrome extension manifest currently declares:

- `http://*/*`
- `https://*/*`

The content script match surface is also broad:

- `http://*/*`
- `https://*/*`

This package records the current state for release review. It does not approve public release by itself.

## Why Permissions Are Broad

The current extension line was validated as an installed/local evidence candidate where browser-side observation may need to run across arbitrary HTTP and HTTPS pages during manual QA and future controlled workflows.

The permission shape supports broad inspection capability, but that same breadth increases the burden for a public release.

## Release Risk

Broad host permissions create these public-release risks:

- Chrome Web Store review scrutiny.
- User trust and install friction.
- Privacy disclosure requirements.
- Support burden if users infer always-on page access.
- Need for clear product copy explaining disabled runtime/provider state.

## Dependent Functionality

The broad permissions may support:

- Installed extension manual QA across pages.
- Future page-context observation.
- Future browser workflow planning.
- Future local-first evidence capture.

No runtime productive execution, browser automation, provider/cloud, filesystem, or capability unlock is enabled by M645.

## Why They Are Not Changed In M645

Changing host permissions would be a product manifest change and would require a dedicated implementation milestone, installed extension reload QA, CSP/permission regression review, and release evidence refresh.

M645 is intentionally documentation/proof-only.

## Future Narrowing Options

Option A: retain broad permissions with strong public-release justification.

Option B: narrow permissions before public release through a dedicated manifest patch milestone.

Option C: split release posture into an internal/local-first candidate and a future public SKU/build with narrower permissions.

Recommended current posture: `justified_for_internal_candidate` and `open_for_public_release`.

## Allowed Release Scope

Allowed after M645:

- Internal evidence release.
- Local-first candidate review.
- Release evidence continuation.

Not allowed after M645:

- Automatic public release.
- Provider/cloud release.
- Runtime productive release.
- Filesystem or browser automation release.

## Public Release Closure Criteria

Public release requires one of:

- strong host permission justification approved for store/public release,
- reduced permissions with full regression evidence,
- separate internal and public SKU/build decision.

Until then, public release remains `NO-GO`.
